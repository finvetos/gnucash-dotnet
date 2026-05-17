using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Books;

/// <summary>
/// Reads the stable read-only portions of a GnuCash XML book.
/// </summary>
public sealed class GnuCashBookReader
{
    public GnuCashBookSummary Open(string bookPath)
    {
        var snapshot = ReadSnapshot(bookPath);

        return new GnuCashBookSummary(
            snapshot.BookPath,
            snapshot.FileFormat,
            snapshot.BookId,
            snapshot.Commodities.Count,
            snapshot.Accounts.Count,
            snapshot.Transactions.Count,
            snapshot.Transactions.Sum(transaction => transaction.Splits.Count),
            snapshot.Prices.Count);
    }

    public IReadOnlyList<GnuCashCommodityRecord> ListCommodities(string bookPath) =>
        ReadSnapshot(bookPath).Commodities;

    public IReadOnlyList<GnuCashAccountRecord> ListAccounts(string bookPath) =>
        ReadSnapshot(bookPath).Accounts;

    public IReadOnlyList<GnuCashTransactionRecord> ListTransactions(string bookPath) =>
        ReadSnapshot(bookPath).Transactions;

    public IReadOnlyList<GnuCashPriceRecord> ListPrices(string bookPath) =>
        ReadSnapshot(bookPath).Prices;

    private static GnuCashBookSnapshot ReadSnapshot(string bookPath)
    {
        if (string.IsNullOrWhiteSpace(bookPath))
        {
            throw new ArgumentException("A GnuCash book path is required.", nameof(bookPath));
        }

        var fullPath = Path.GetFullPath(bookPath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"The GnuCash book file does not exist: {fullPath}");
        }

        using var stream = OpenBookStream(fullPath);
        var document = XDocument.Load(stream, LoadOptions.None);
        var book = document
            .Descendants()
            .FirstOrDefault(element => HasLocalName(element, "book"));

        if (book is null)
        {
            throw new InvalidOperationException("The file does not look like a GnuCash XML book.");
        }

        var commodities = book.Elements()
            .Where(element => HasLocalName(element, "commodity"))
            .Select(ReadCommodity)
            .ToArray();
        var accounts = book.Elements()
            .Where(element => HasLocalName(element, "account"))
            .Select(ReadAccount)
            .ToArray();
        var transactions = book.Elements()
            .Where(element => HasLocalName(element, "transaction"))
            .Select(ReadTransaction)
            .ToArray();
        var prices = book.Elements()
            .Where(element => HasLocalName(element, "pricedb"))
            .SelectMany(priceDb => priceDb.Descendants().Where(element => HasLocalName(element, "price")))
            .Select(ReadPrice)
            .ToArray();

        return new GnuCashBookSnapshot(
            fullPath,
            "GnuCashXml",
            ReadChildText(book, "id"),
            commodities,
            accounts,
            transactions,
            prices);
    }

    private static Stream OpenBookStream(string fullPath)
    {
        var file = File.OpenRead(fullPath);
        var header = new byte[16];
        var read = file.Read(header, 0, header.Length);
        file.Position = 0;

        if (read >= 2 && header[0] == 0x1f && header[1] == 0x8b)
        {
            return new GZipStream(file, CompressionMode.Decompress);
        }

        if (read >= 16 && IsSqliteHeader(header))
        {
            file.Dispose();
            throw new InvalidOperationException(
                "SQLite GnuCash books are not supported by the bootstrap XML reader yet.");
        }

        return file;
    }

    private static bool IsSqliteHeader(byte[] header)
    {
        const string sqliteHeader = "SQLite format 3";
        return string.Equals(
            System.Text.Encoding.ASCII.GetString(header, 0, sqliteHeader.Length),
            sqliteHeader,
            StringComparison.Ordinal);
    }

    private static GnuCashCommodityRecord ReadCommodity(XElement commodity)
    {
        var space = RequireChildText(commodity, "space", "commodity space");
        var id = RequireChildText(commodity, "id", "commodity id");

        return new GnuCashCommodityRecord(
            space,
            id,
            ReadChildText(commodity, "name"),
            ReadChildText(commodity, "xcode"),
            ParseInt32(ReadChildText(commodity, "fraction")));
    }

    private static GnuCashAccountRecord ReadAccount(XElement account)
    {
        var commodity = Child(account, "commodity");

        return new GnuCashAccountRecord(
            RequireChildText(account, "id", "account id"),
            RequireChildText(account, "name", "account name"),
            RequireChildText(account, "type", "account type"),
            ReadChildText(account, "parent"),
            ReadChildText(commodity, "space"),
            ReadChildText(commodity, "id"),
            ReadChildText(account, "code"),
            ReadChildText(account, "description"),
            ReadBooleanSlot(account, "placeholder"));
    }

    private static GnuCashTransactionRecord ReadTransaction(XElement transaction)
    {
        var currency = Child(transaction, "currency");
        var splits = Child(transaction, "splits")?
            .Elements()
            .Where(element => HasLocalName(element, "split"))
            .Select(ReadSplit)
            .ToArray() ?? [];

        return new GnuCashTransactionRecord(
            RequireChildText(transaction, "id", "transaction id"),
            ReadChildText(transaction, "num"),
            ReadChildText(transaction, "description"),
            ReadChildText(currency, "space"),
            ReadChildText(currency, "id"),
            ReadTimestamp(transaction, "date-posted"),
            ReadTimestamp(transaction, "date-entered"),
            splits);
    }

    private static GnuCashSplitRecord ReadSplit(XElement split)
    {
        return new GnuCashSplitRecord(
            RequireChildText(split, "id", "split id"),
            RequireChildText(split, "account", "split account"),
            ReadChildText(split, "memo"),
            ReadChildText(split, "action"),
            ReadChildText(split, "reconciled-state") ?? "n",
            ReadAmount(ReadChildText(split, "value")),
            ReadAmount(ReadChildText(split, "quantity")),
            ReadTimestamp(split, "reconcile-date"));
    }

    private static GnuCashPriceRecord ReadPrice(XElement price)
    {
        var commodity = Child(price, "commodity");
        var currency = Child(price, "currency");
        if (commodity is null || currency is null)
        {
            throw new InvalidOperationException("The GnuCash XML book has a price entry without commodity or currency.");
        }

        return new GnuCashPriceRecord(
            RequireChildText(price, "id", "price id"),
            RequireChildText(commodity, "space", "price commodity space"),
            RequireChildText(commodity, "id", "price commodity id"),
            RequireChildText(currency, "space", "price currency space"),
            RequireChildText(currency, "id", "price currency id"),
            ReadTimestamp(price, "time"),
            ReadChildText(price, "source"),
            ReadChildText(price, "type"),
            ReadAmount(ReadChildText(price, "value")));
    }

    private static GnuCashAmountRecord ReadAmount(string? rawValue)
    {
        rawValue = rawValue?.Trim() ?? string.Empty;
        var parts = rawValue.Split('/', 2, StringSplitOptions.TrimEntries);

        if (parts.Length == 2 &&
            long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var numerator) &&
            long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var denominator))
        {
            return new GnuCashAmountRecord(rawValue, numerator, denominator);
        }

        return new GnuCashAmountRecord(rawValue, null, null);
    }

    private static DateTimeOffset? ReadTimestamp(XElement element, string childLocalName)
    {
        var dateText = Child(element, childLocalName)?
            .Descendants()
            .FirstOrDefault(candidate => HasLocalName(candidate, "date"))
            ?.Value
            .Trim();

        if (string.IsNullOrWhiteSpace(dateText))
        {
            return null;
        }

        if (dateText.Length >= 5 &&
            (dateText[^5] == '+' || dateText[^5] == '-') &&
            dateText[^3] != ':')
        {
            dateText = string.Concat(dateText.AsSpan(0, dateText.Length - 2), ":", dateText.AsSpan(dateText.Length - 2));
        }

        return DateTimeOffset.TryParse(
            dateText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed)
            ? parsed
            : null;
    }

    private static bool ReadBooleanSlot(XElement element, string key)
    {
        var slot = Child(element, "slots")?
            .Elements()
            .FirstOrDefault(candidate =>
                HasLocalName(candidate, "slot") &&
                string.Equals(ReadChildText(candidate, "key"), key, StringComparison.OrdinalIgnoreCase));
        var value = ReadChildText(slot, "value");

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static string RequireChildText(XElement element, string localName, string displayName) =>
        ReadChildText(element, localName) ??
        throw new InvalidOperationException($"The GnuCash XML book is missing {displayName}.");

    private static string? ReadChildText(XElement? element, string localName) =>
        Child(element, localName)?.Value.Trim();

    private static XElement? Child(XElement? element, string localName) =>
        element?.Elements().FirstOrDefault(candidate => HasLocalName(candidate, localName));

    private static bool HasLocalName(XElement element, string localName) =>
        string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal);

    private static int ParseInt32(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private sealed record GnuCashBookSnapshot(
        string BookPath,
        string FileFormat,
        string? BookId,
        IReadOnlyList<GnuCashCommodityRecord> Commodities,
        IReadOnlyList<GnuCashAccountRecord> Accounts,
        IReadOnlyList<GnuCashTransactionRecord> Transactions,
        IReadOnlyList<GnuCashPriceRecord> Prices);
}
