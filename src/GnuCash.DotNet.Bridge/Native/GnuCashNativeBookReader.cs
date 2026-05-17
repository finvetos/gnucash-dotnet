using System.Globalization;
using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Reads book data through the installed native GnuCash engine.
/// </summary>
public sealed class GnuCashNativeBookReader
{
    private readonly GnuCashNativeRuntime runtime;

    public GnuCashNativeBookReader()
        : this(new GnuCashNativeRuntime())
    {
    }

    internal GnuCashNativeBookReader(GnuCashNativeRuntime runtime)
    {
        this.runtime = runtime;
    }

    internal GnuCashNativeBookReadResult ReadCoreBookData(
        string bookPath,
        string? explicitInstallPath = null)
    {
        var pathRead = NormalizeBookPath(bookPath);
        if (!pathRead.IsReady)
        {
            return GnuCashNativeBookReadResult.Failed(null, pathRead.BookPath, pathRead.Message);
        }

        var runtimeState = runtime.Prepare(explicitInstallPath);
        if (!runtimeState.IsPrepared)
        {
            return GnuCashNativeBookReadResult.Failed(
                runtimeState.ApiStatus,
                pathRead.BookPath,
                runtimeState.ApiStatus.Message);
        }

        return ReadPrepared(runtimeState.ApiStatus, pathRead.BookPath);
    }

    private static GnuCashNativeBookReadResult ReadPrepared(
        GnuCashNativeApiStatus apiStatus,
        string normalizedBookPath)
    {
        try
        {
            using var handle = GnuCashNativeSessionHandle.OpenReadOnly(normalizedBookPath);
            var accounts = ReadAccounts(handle.RootAccount);
            var transactions = ReadTransactions(handle.RootAccount);
            var prices = ReadPrices(handle.Book);
            return GnuCashNativeBookReadResult.Ready(
                apiStatus,
                normalizedBookPath,
                ReadGuid(handle.Book),
                accounts,
                ReadCommodities(handle.Book, GetReferencedCommodityKeys(accounts, transactions, prices)),
                transactions,
                prices);
        }
        catch (GnuCashNativeOperationException ex)
        {
            return GnuCashNativeBookReadResult.Failed(
                apiStatus,
                normalizedBookPath,
                ex.BackendErrorMessage ?? ex.Message);
        }
        catch (Exception ex) when (
            ex is DllNotFoundException or
                  EntryPointNotFoundException or
                  BadImageFormatException or
                  SEHException or
                  InvalidOperationException)
        {
            return GnuCashNativeBookReadResult.Failed(
                apiStatus,
                normalizedBookPath,
                "The native GnuCash read failed: " + ex.Message);
        }
    }

    private static GnuCashNativeBookPathRead NormalizeBookPath(string bookPath)
    {
        if (string.IsNullOrWhiteSpace(bookPath))
        {
            return new GnuCashNativeBookPathRead(false, string.Empty, "A GnuCash book path is required.");
        }

        try
        {
            var normalizedBookPath = Path.GetFullPath(bookPath);
            return File.Exists(normalizedBookPath)
                ? new GnuCashNativeBookPathRead(true, normalizedBookPath, string.Empty)
                : new GnuCashNativeBookPathRead(false, normalizedBookPath, "The requested GnuCash book does not exist.");
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new GnuCashNativeBookPathRead(false, bookPath, "The GnuCash book path is invalid: " + ex.Message);
        }
    }

    private static IReadOnlyList<GnuCashAccountRecord> ReadAccounts(IntPtr rootAccount)
    {
        var accounts = new List<GnuCashAccountRecord>();
        AppendAccount(rootAccount, accounts);
        return accounts;
    }

    private static void AppendAccount(IntPtr account, List<GnuCashAccountRecord> accounts)
    {
        if (account == IntPtr.Zero)
        {
            return;
        }

        accounts.Add(ReadAccount(account));
        var childCount = GnuCashNativeMethods.gnc_account_n_children(account);
        for (var index = 0; index < childCount; index++)
        {
            AppendAccount(GnuCashNativeMethods.gnc_account_nth_child(account, index), accounts);
        }
    }

    private static GnuCashAccountRecord ReadAccount(IntPtr account)
    {
        var commodity = GnuCashNativeMethods.xaccAccountGetCommodity(account);
        var accountType = GnuCashNativeMethods.xaccAccountGetType(account);

        return new GnuCashAccountRecord(
            ReadGuid(account) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.xaccAccountGetName(account)) ?? string.Empty,
            ReadAccountType(accountType),
            ReadParentId(account),
            ReadCommoditySpace(commodity),
            ReadCommodityId(commodity),
            ReadBorrowedString(GnuCashNativeMethods.xaccAccountGetCode(account)),
            ReadBorrowedString(GnuCashNativeMethods.xaccAccountGetDescription(account)),
            GnuCashNativeMethods.xaccAccountGetPlaceholder(account) != 0);
    }

    private static IReadOnlyList<GnuCashCommodityRecord> ReadCommodities(
        IntPtr book,
        IReadOnlySet<string> referencedCommodityKeys)
    {
        var table = book == IntPtr.Zero
            ? IntPtr.Zero
            : GnuCashNativeMethods.gnc_commodity_table_get_table(book);
        if (table == IntPtr.Zero)
        {
            return [];
        }

        var commodities = new List<GnuCashCommodityRecord>();
        GncCommodityTableForeachCallback callback = (commodity, _) =>
        {
            if (commodity != IntPtr.Zero)
            {
                var record = ReadCommodity(commodity);
                if (ShouldIncludeCommodity(record, referencedCommodityKeys))
                {
                    commodities.Add(record);
                }
            }

            return 1;
        };

        _ = GnuCashNativeMethods.gnc_commodity_table_foreach_commodity(table, callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        return commodities
            .OrderBy(commodity => commodity.Space, StringComparer.Ordinal)
            .ThenBy(commodity => commodity.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlySet<string> GetReferencedCommodityKeys(
        IReadOnlyList<GnuCashAccountRecord> accounts,
        IReadOnlyList<GnuCashTransactionRecord> transactions,
        IReadOnlyList<GnuCashPriceRecord> prices)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var account in accounts)
        {
            AddCommodityKey(keys, account.CommoditySpace, account.CommodityId);
        }

        foreach (var transaction in transactions)
        {
            AddCommodityKey(keys, transaction.CurrencySpace, transaction.CurrencyId);
        }

        foreach (var price in prices)
        {
            AddCommodityKey(keys, price.CommoditySpace, price.CommodityId);
            AddCommodityKey(keys, price.CurrencySpace, price.CurrencyId);
        }

        return keys;
    }

    private static void AddCommodityKey(HashSet<string> keys, string? space, string? id)
    {
        if (!string.IsNullOrWhiteSpace(space) && !string.IsNullOrWhiteSpace(id))
        {
            keys.Add(CommodityKey(space, id));
        }
    }

    private static bool ShouldIncludeCommodity(
        GnuCashCommodityRecord commodity,
        IReadOnlySet<string> referencedCommodityKeys)
    {
        if (referencedCommodityKeys.Contains(CommodityKey(commodity.Space, commodity.Id)))
        {
            return true;
        }

        return !string.Equals(commodity.Space, "CURRENCY", StringComparison.Ordinal) &&
               !string.Equals(commodity.Space, "template", StringComparison.Ordinal);
    }

    private static GnuCashCommodityRecord ReadCommodity(IntPtr commodity) =>
        new(
            ReadCommoditySpace(commodity) ?? string.Empty,
            ReadCommodityId(commodity) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_fullname(commodity)),
            ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_cusip(commodity)),
            GnuCashNativeMethods.gnc_commodity_get_fraction(commodity));

    private static IReadOnlyList<GnuCashTransactionRecord> ReadTransactions(IntPtr rootAccount)
    {
        if (rootAccount == IntPtr.Zero)
        {
            return [];
        }

        var transactions = new List<GnuCashTransactionRecord>();
        GncTransactionCallback callback = (transaction, _) =>
        {
            if (transaction != IntPtr.Zero)
            {
                transactions.Add(ReadTransaction(transaction));
            }

            return 0;
        };

        var result = GnuCashNativeMethods.xaccAccountTreeForEachTransaction(rootAccount, callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        if (result != 0)
        {
            throw new InvalidOperationException("The native transaction traversal stopped before completion.");
        }

        return transactions
            .OrderBy(transaction => transaction.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static GnuCashTransactionRecord ReadTransaction(IntPtr transaction)
    {
        var currency = GnuCashNativeMethods.xaccTransGetCurrency(transaction);
        var splitCount = GnuCashNativeMethods.xaccTransCountSplits(transaction);
        var splits = new List<GnuCashSplitRecord>(Math.Max(splitCount, 0));
        for (var index = 0; index < splitCount; index++)
        {
            var split = GnuCashNativeMethods.xaccTransGetSplit(transaction, index);
            if (split != IntPtr.Zero)
            {
                splits.Add(ReadSplit(split));
            }
        }

        return new GnuCashTransactionRecord(
            ReadGuid(transaction) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.xaccTransGetNum(transaction)),
            ReadBorrowedString(GnuCashNativeMethods.xaccTransGetDescription(transaction)),
            ReadCommoditySpace(currency),
            ReadCommodityId(currency),
            ReadTime(GnuCashNativeMethods.xaccTransGetDate(transaction)),
            ReadTime(GnuCashNativeMethods.xaccTransGetDateEntered(transaction)),
            splits.OrderBy(split => split.Id, StringComparer.Ordinal).ToArray());
    }

    private static GnuCashSplitRecord ReadSplit(IntPtr split)
    {
        var account = GnuCashNativeMethods.xaccSplitGetAccount(split);

        return new GnuCashSplitRecord(
            ReadGuid(split) ?? string.Empty,
            account == IntPtr.Zero ? string.Empty : ReadGuid(account) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.xaccSplitGetMemo(split)),
            ReadBorrowedString(GnuCashNativeMethods.xaccSplitGetAction(split)),
            ReadReconciledState(GnuCashNativeMethods.xaccSplitGetReconcile(split)),
            ReadAmount(GnuCashNativeMethods.xaccSplitGetValue(split)),
            ReadAmount(GnuCashNativeMethods.xaccSplitGetAmount(split)),
            ReadTime(GnuCashNativeMethods.xaccSplitGetDateReconciled(split)));
    }

    private static IReadOnlyList<GnuCashPriceRecord> ReadPrices(IntPtr book)
    {
        var priceDb = book == IntPtr.Zero ? IntPtr.Zero : GnuCashNativeMethods.gnc_pricedb_get_db(book);
        if (priceDb == IntPtr.Zero)
        {
            return [];
        }

        var prices = new List<GnuCashPriceRecord>();
        GncPriceForeachCallback callback = (price, _) =>
        {
            if (price != IntPtr.Zero)
            {
                prices.Add(ReadPrice(price));
            }

            return 1;
        };

        _ = GnuCashNativeMethods.gnc_pricedb_foreach_price(priceDb, callback, IntPtr.Zero, stableOrder: 1);
        GC.KeepAlive(callback);

        return prices
            .OrderBy(price => price.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static GnuCashPriceRecord ReadPrice(IntPtr price)
    {
        var commodity = GnuCashNativeMethods.gnc_price_get_commodity(price);
        var currency = GnuCashNativeMethods.gnc_price_get_currency(price);

        return new GnuCashPriceRecord(
            ReadGuid(price) ?? string.Empty,
            ReadCommoditySpace(commodity) ?? string.Empty,
            ReadCommodityId(commodity) ?? string.Empty,
            ReadCommoditySpace(currency) ?? string.Empty,
            ReadCommodityId(currency) ?? string.Empty,
            ReadTime(GnuCashNativeMethods.gnc_price_get_time64(price)),
            ReadBorrowedString(GnuCashNativeMethods.gnc_price_get_source_string(price)),
            ReadBorrowedString(GnuCashNativeMethods.gnc_price_get_typestr(price)),
            ReadAmount(GnuCashNativeMethods.gnc_price_get_value(price)));
    }

    private static GnuCashAmountRecord ReadAmount(GncNumeric value) =>
        new(
            value.Numerator.ToString(CultureInfo.InvariantCulture) + "/" +
            value.Denominator.ToString(CultureInfo.InvariantCulture),
            value.Numerator,
            value.Denominator);

    private static DateTimeOffset? ReadTime(long seconds) =>
        seconds == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(seconds);

    private static string ReadReconciledState(byte value) =>
        value == 0 ? "n" : ((char)value).ToString();

    private static string ReadAccountType(int accountType) =>
        ReadBorrowedString(GnuCashNativeMethods.xaccAccountTypeEnumAsString(accountType)) ??
        accountType.ToString(CultureInfo.InvariantCulture);

    private static string? ReadParentId(IntPtr account)
    {
        var parent = GnuCashNativeMethods.gnc_account_get_parent(account);
        return parent == IntPtr.Zero ? null : ReadGuid(parent);
    }

    private static string? ReadCommoditySpace(IntPtr commodity) =>
        commodity == IntPtr.Zero
            ? null
            : ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_namespace(commodity));

    private static string? ReadCommodityId(IntPtr commodity) =>
        commodity == IntPtr.Zero
            ? null
            : ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_mnemonic(commodity));

    private static string CommodityKey(string space, string id) =>
        space + "::" + id;

    private static string? ReadGuid(IntPtr instance)
    {
        var guid = GnuCashNativeMethods.qof_instance_get_guid(instance);
        if (guid == IntPtr.Zero)
        {
            return null;
        }

        var text = GnuCashNativeMethods.guid_to_string(guid);
        try
        {
            return GnuCashNativeMethods.PtrToUtf8String(text);
        }
        finally
        {
            if (text != IntPtr.Zero)
            {
                GnuCashNativeMethods.g_free(text);
            }
        }
    }

    private static string? ReadBorrowedString(IntPtr value) =>
        GnuCashNativeMethods.PtrToUtf8String(value);
}

internal sealed record GnuCashNativeBookReadResult(
    GnuCashNativeApiStatus? ApiStatus,
    string BookPath,
    string? BookId,
    IReadOnlyList<GnuCashAccountRecord> Accounts,
    IReadOnlyList<GnuCashCommodityRecord> Commodities,
    IReadOnlyList<GnuCashTransactionRecord> Transactions,
    IReadOnlyList<GnuCashPriceRecord> Prices,
    bool IsReady,
    string Message)
{
    public int SplitCount => Transactions.Sum(transaction => transaction.Splits.Count);

    public static GnuCashNativeBookReadResult Ready(
        GnuCashNativeApiStatus apiStatus,
        string bookPath,
        string? bookId,
        IReadOnlyList<GnuCashAccountRecord> accounts,
        IReadOnlyList<GnuCashCommodityRecord> commodities,
        IReadOnlyList<GnuCashTransactionRecord> transactions,
        IReadOnlyList<GnuCashPriceRecord> prices) =>
        new(apiStatus, bookPath, bookId, accounts, commodities, transactions, prices, true, "The native GnuCash read completed.");

    public static GnuCashNativeBookReadResult Failed(
        GnuCashNativeApiStatus? apiStatus,
        string bookPath,
        string message) =>
        new(apiStatus, bookPath, null, [], [], [], [], false, message);
}

internal sealed record GnuCashNativeBookPathRead(bool IsReady, string BookPath, string Message);
