using System.Runtime.InteropServices;
using GnuCash.DotNet.Bridge.Books;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Compares native read results against the bootstrap XML reader while native coverage grows.
/// </summary>
public sealed class GnuCashNativeReadParityChecker
{
    private readonly GnuCashNativeBookReader nativeReader;
    private readonly GnuCashBookReader xmlReader;

    public GnuCashNativeReadParityChecker()
        : this(new GnuCashNativeBookReader(), new GnuCashBookReader())
    {
    }

    internal GnuCashNativeReadParityChecker(
        GnuCashNativeBookReader nativeReader,
        GnuCashBookReader xmlReader)
    {
        this.nativeReader = nativeReader;
        this.xmlReader = xmlReader;
    }

    public GnuCashNativeReadParityStatus Validate(
        string bookPath,
        string? explicitInstallPath = null)
    {
        var nativeRead = nativeReader.ReadCoreBookData(bookPath, explicitInstallPath);
        if (!nativeRead.IsReady)
        {
            return CreateStatus(nativeRead, [], [], [], [], [], nativeRead.Message);
        }

        XmlBookRead xmlRead;
        try
        {
            xmlRead = new XmlBookRead(
                xmlReader.Open(nativeRead.BookPath),
                xmlReader.ListAccounts(nativeRead.BookPath),
                xmlReader.ListCommodities(nativeRead.BookPath),
                xmlReader.ListTransactions(nativeRead.BookPath),
                xmlReader.ListPrices(nativeRead.BookPath));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return CreateStatus(
                nativeRead,
                [],
                [],
                [],
                [],
                [],
                "The XML bootstrap reader could not inspect the book: " + ex.Message);
        }

        var bookMismatches = CompareBook(nativeRead, xmlRead.Summary);
        var accountMismatches = CompareAccounts(nativeRead.Accounts, xmlRead.Accounts);
        var commodityMismatches = CompareCommodities(nativeRead.Commodities, xmlRead.Commodities);
        var transactionMismatches = CompareTransactions(nativeRead.Transactions, xmlRead.Transactions);
        var priceMismatches = ComparePrices(nativeRead.Prices, xmlRead.Prices);
        var hasMismatches = HasMismatches(
            bookMismatches,
            accountMismatches,
            commodityMismatches,
            transactionMismatches,
            priceMismatches);
        var message = hasMismatches
            ? "Native core book reads differ from the XML bootstrap reader."
            : "Native core book reads match the XML bootstrap reader.";

        return CreateStatus(
            nativeRead,
            bookMismatches,
            accountMismatches,
            commodityMismatches,
            transactionMismatches,
            priceMismatches,
            message,
            xmlRead);
    }

    private static GnuCashNativeReadParityStatus CreateStatus(
        GnuCashNativeBookReadResult nativeRead,
        IReadOnlyList<string> bookMismatches,
        IReadOnlyList<string> accountMismatches,
        IReadOnlyList<string> commodityMismatches,
        IReadOnlyList<string> transactionMismatches,
        IReadOnlyList<string> priceMismatches,
        string message,
        XmlBookRead? xmlRead = null)
    {
        var apiStatus = nativeRead.ApiStatus;
        var isReady = nativeRead.IsReady &&
                      !HasMismatches(
                          bookMismatches,
                          accountMismatches,
                          commodityMismatches,
                          transactionMismatches,
                          priceMismatches);

        return new GnuCashNativeReadParityStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: apiStatus?.CanCallFromCurrentProcess ?? false,
            InstallPath: apiStatus?.InstallPath,
            DisplayVersion: apiStatus?.DisplayVersion,
            BookPath: nativeRead.BookPath,
            EnginePath: apiStatus?.EnginePath,
            ProcessArchitecture: apiStatus?.ProcessArchitecture ?? RuntimeInformation.ProcessArchitecture.ToString(),
            NativeBookId: nativeRead.BookId,
            XmlBookId: xmlRead?.Summary.BookId,
            NativeAccountCount: nativeRead.IsReady ? nativeRead.Accounts.Count : null,
            XmlAccountCount: xmlRead?.Accounts.Count,
            NativeCommodityCount: nativeRead.IsReady ? nativeRead.Commodities.Count : null,
            XmlCommodityCount: xmlRead?.Commodities.Count,
            NativeTransactionCount: nativeRead.IsReady ? nativeRead.Transactions.Count : null,
            XmlTransactionCount: xmlRead?.Transactions.Count,
            NativeSplitCount: nativeRead.IsReady ? nativeRead.SplitCount : null,
            XmlSplitCount: xmlRead?.Summary.SplitCount,
            NativePriceCount: nativeRead.IsReady ? nativeRead.Prices.Count : null,
            XmlPriceCount: xmlRead?.Prices.Count,
            BookMismatches: bookMismatches,
            AccountMismatches: accountMismatches,
            CommodityMismatches: commodityMismatches,
            TransactionMismatches: transactionMismatches,
            PriceMismatches: priceMismatches,
            CheckedPaths: apiStatus?.CheckedPaths ?? [],
            Message: message);
    }

    private static bool HasMismatches(params IReadOnlyList<string>[] mismatchGroups) =>
        mismatchGroups.Any(group => group.Count > 0);

    private static IReadOnlyList<string> CompareBook(
        GnuCashNativeBookReadResult nativeRead,
        GnuCashBookSummary xmlSummary)
    {
        var mismatches = new List<string>();
        AddMismatch(mismatches, "Book", "id", nativeRead.BookId, xmlSummary.BookId);
        return mismatches;
    }

    private static IReadOnlyList<string> CompareAccounts(
        IReadOnlyList<GnuCashAccountRecord> nativeAccounts,
        IReadOnlyList<GnuCashAccountRecord> xmlAccounts)
    {
        var mismatches = new List<string>();
        var nativeById = nativeAccounts.ToDictionary(account => account.Id, StringComparer.Ordinal);

        foreach (var expected in xmlAccounts.OrderBy(account => account.Id, StringComparer.Ordinal))
        {
            if (!nativeById.TryGetValue(expected.Id, out var actual))
            {
                mismatches.Add($"Account {expected.Id} is missing from native reads.");
                continue;
            }

            CompareAccountFields(mismatches, actual, expected);
        }

        foreach (var extra in nativeById.Keys.Except(xmlAccounts.Select(account => account.Id), StringComparer.Ordinal))
        {
            mismatches.Add($"Account {extra} is only present in native reads.");
        }

        return mismatches;
    }

    private static void CompareAccountFields(
        List<string> mismatches,
        GnuCashAccountRecord actual,
        GnuCashAccountRecord expected)
    {
        AddMismatch(mismatches, actual.Id, "name", actual.Name, expected.Name);
        AddMismatch(mismatches, actual.Id, "type", actual.Type, expected.Type);
        AddMismatch(mismatches, actual.Id, "parent", actual.ParentId, expected.ParentId);
        AddMismatch(mismatches, actual.Id, "commodity space", actual.CommoditySpace, expected.CommoditySpace);
        AddMismatch(mismatches, actual.Id, "commodity id", actual.CommodityId, expected.CommodityId);
        AddMismatch(mismatches, actual.Id, "code", actual.Code, expected.Code);
        AddMismatch(mismatches, actual.Id, "description", actual.Description, expected.Description);

        if (actual.IsPlaceholder != expected.IsPlaceholder)
        {
            mismatches.Add(
                $"Account {actual.Id} placeholder differs: native={actual.IsPlaceholder}, xml={expected.IsPlaceholder}.");
        }
    }

    private static IReadOnlyList<string> CompareCommodities(
        IReadOnlyList<GnuCashCommodityRecord> nativeCommodities,
        IReadOnlyList<GnuCashCommodityRecord> xmlCommodities)
    {
        var mismatches = new List<string>();
        var nativeByKey = nativeCommodities.ToDictionary(CommodityKey, StringComparer.Ordinal);

        foreach (var expected in xmlCommodities.OrderBy(CommodityKey, StringComparer.Ordinal))
        {
            var key = CommodityKey(expected);
            if (!nativeByKey.TryGetValue(key, out var actual))
            {
                mismatches.Add($"Commodity {key} is missing from native reads.");
                continue;
            }

            AddMismatch(mismatches, key, "name", actual.Name, expected.Name);
            AddMismatch(mismatches, key, "xcode", actual.XCode, expected.XCode);
            if (actual.Fraction != expected.Fraction)
            {
                mismatches.Add(
                    $"Commodity {key} fraction differs: native={actual.Fraction}, xml={expected.Fraction}.");
            }
        }

        foreach (var extra in nativeByKey.Keys.Except(xmlCommodities.Select(CommodityKey), StringComparer.Ordinal))
        {
            mismatches.Add($"Commodity {extra} is only present in native reads.");
        }

        return mismatches;
    }

    private static IReadOnlyList<string> CompareTransactions(
        IReadOnlyList<GnuCashTransactionRecord> nativeTransactions,
        IReadOnlyList<GnuCashTransactionRecord> xmlTransactions)
    {
        var mismatches = new List<string>();
        var nativeById = nativeTransactions.ToDictionary(transaction => transaction.Id, StringComparer.Ordinal);

        foreach (var expected in xmlTransactions.OrderBy(transaction => transaction.Id, StringComparer.Ordinal))
        {
            if (!nativeById.TryGetValue(expected.Id, out var actual))
            {
                mismatches.Add($"Transaction {expected.Id} is missing from native reads.");
                continue;
            }

            CompareTransactionFields(mismatches, actual, expected);
        }

        foreach (var extra in nativeById.Keys.Except(xmlTransactions.Select(transaction => transaction.Id), StringComparer.Ordinal))
        {
            mismatches.Add($"Transaction {extra} is only present in native reads.");
        }

        return mismatches;
    }

    private static void CompareTransactionFields(
        List<string> mismatches,
        GnuCashTransactionRecord actual,
        GnuCashTransactionRecord expected)
    {
        AddMismatch(mismatches, actual.Id, "number", actual.Number, expected.Number);
        AddMismatch(mismatches, actual.Id, "description", actual.Description, expected.Description);
        AddMismatch(mismatches, actual.Id, "currency space", actual.CurrencySpace, expected.CurrencySpace);
        AddMismatch(mismatches, actual.Id, "currency id", actual.CurrencyId, expected.CurrencyId);
        AddMismatch(mismatches, actual.Id, "posted date", actual.PostedAt, expected.PostedAt);
        AddMismatch(mismatches, actual.Id, "entered date", actual.EnteredAt, expected.EnteredAt);
        CompareSplits(mismatches, actual.Id, actual.Splits, expected.Splits);
    }

    private static void CompareSplits(
        List<string> mismatches,
        string transactionId,
        IReadOnlyList<GnuCashSplitRecord> actualSplits,
        IReadOnlyList<GnuCashSplitRecord> expectedSplits)
    {
        var nativeById = actualSplits.ToDictionary(split => split.Id, StringComparer.Ordinal);
        foreach (var expected in expectedSplits.OrderBy(split => split.Id, StringComparer.Ordinal))
        {
            if (!nativeById.TryGetValue(expected.Id, out var actual))
            {
                mismatches.Add($"Split {expected.Id} in transaction {transactionId} is missing from native reads.");
                continue;
            }

            CompareSplitFields(mismatches, actual, expected);
        }

        foreach (var extra in nativeById.Keys.Except(expectedSplits.Select(split => split.Id), StringComparer.Ordinal))
        {
            mismatches.Add($"Split {extra} in transaction {transactionId} is only present in native reads.");
        }
    }

    private static void CompareSplitFields(
        List<string> mismatches,
        GnuCashSplitRecord actual,
        GnuCashSplitRecord expected)
    {
        AddMismatch(mismatches, actual.Id, "account", actual.AccountId, expected.AccountId);
        AddMismatch(mismatches, actual.Id, "memo", actual.Memo, expected.Memo);
        AddMismatch(mismatches, actual.Id, "action", actual.Action, expected.Action);
        AddMismatch(mismatches, actual.Id, "reconciled state", actual.ReconciledState, expected.ReconciledState);
        AddMismatch(mismatches, actual.Id, "value", actual.Value, expected.Value);
        AddMismatch(mismatches, actual.Id, "quantity", actual.Quantity, expected.Quantity);
        AddMismatch(mismatches, actual.Id, "reconcile date", actual.ReconciledAt, expected.ReconciledAt);
    }

    private static IReadOnlyList<string> ComparePrices(
        IReadOnlyList<GnuCashPriceRecord> nativePrices,
        IReadOnlyList<GnuCashPriceRecord> xmlPrices)
    {
        var mismatches = new List<string>();
        var nativeById = nativePrices.ToDictionary(price => price.Id, StringComparer.Ordinal);

        foreach (var expected in xmlPrices.OrderBy(price => price.Id, StringComparer.Ordinal))
        {
            if (!nativeById.TryGetValue(expected.Id, out var actual))
            {
                mismatches.Add($"Price {expected.Id} is missing from native reads.");
                continue;
            }

            ComparePriceFields(mismatches, actual, expected);
        }

        foreach (var extra in nativeById.Keys.Except(xmlPrices.Select(price => price.Id), StringComparer.Ordinal))
        {
            mismatches.Add($"Price {extra} is only present in native reads.");
        }

        return mismatches;
    }

    private static void ComparePriceFields(
        List<string> mismatches,
        GnuCashPriceRecord actual,
        GnuCashPriceRecord expected)
    {
        AddMismatch(mismatches, actual.Id, "commodity space", actual.CommoditySpace, expected.CommoditySpace);
        AddMismatch(mismatches, actual.Id, "commodity id", actual.CommodityId, expected.CommodityId);
        AddMismatch(mismatches, actual.Id, "currency space", actual.CurrencySpace, expected.CurrencySpace);
        AddMismatch(mismatches, actual.Id, "currency id", actual.CurrencyId, expected.CurrencyId);
        AddMismatch(mismatches, actual.Id, "time", actual.Time, expected.Time);
        AddMismatch(mismatches, actual.Id, "source", actual.Source, expected.Source);
        AddMismatch(mismatches, actual.Id, "type", actual.Type, expected.Type);
        AddMismatch(mismatches, actual.Id, "value", actual.Value, expected.Value);
    }

    private static void AddMismatch(
        List<string> mismatches,
        string identity,
        string field,
        string? actual,
        string? expected)
    {
        if (string.Equals(Normalize(actual), Normalize(expected), StringComparison.Ordinal))
        {
            return;
        }

        mismatches.Add($"{identity} {field} differs: native='{actual}', xml='{expected}'.");
    }

    private static void AddMismatch(
        List<string> mismatches,
        string identity,
        string field,
        DateTimeOffset? actual,
        DateTimeOffset? expected)
    {
        if (actual?.ToUniversalTime() == expected?.ToUniversalTime())
        {
            return;
        }

        mismatches.Add($"{identity} {field} differs: native='{actual}', xml='{expected}'.");
    }

    private static void AddMismatch(
        List<string> mismatches,
        string identity,
        string field,
        GnuCashAmountRecord actual,
        GnuCashAmountRecord expected)
    {
        if (actual.Numerator == expected.Numerator && actual.Denominator == expected.Denominator)
        {
            return;
        }

        mismatches.Add($"{identity} {field} differs: native='{actual.RawValue}', xml='{expected.RawValue}'.");
    }

    private static string CommodityKey(GnuCashCommodityRecord commodity) =>
        commodity.Space + "::" + commodity.Id;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record XmlBookRead(
        GnuCashBookSummary Summary,
        IReadOnlyList<GnuCashAccountRecord> Accounts,
        IReadOnlyList<GnuCashCommodityRecord> Commodities,
        IReadOnlyList<GnuCashTransactionRecord> Transactions,
        IReadOnlyList<GnuCashPriceRecord> Prices);
}
