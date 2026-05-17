using System.Globalization;
using System.Text;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Xml;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Previews CSV price imports without changing the book.
    /// </summary>
    public async Task<GnuCashCsvPriceImportPreview> PreviewCsvPriceImportAsync(
        string csvPath,
        GnuCashCsvPriceImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var fullPath = NormalizeExistingImportPath(csvPath);
        var lines = await File.ReadAllLinesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        if (lines.Length == 0)
        {
            return new GnuCashCsvPriceImportPreview(
                fullPath,
                0,
                0,
                0,
                [],
                [new GnuCashImportIssue(1, "EmptyFile", "The CSV price file is empty.")]);
        }

        var records = lines.Select(ParseCsvLine).ToArray();
        var columnMap = CreatePriceColumnMap(records, options);
        var missing = ValidatePriceColumns(columnMap, options);
        if (missing.Count > 0)
        {
            return new GnuCashCsvPriceImportPreview(fullPath, records.Length - 1, 0, records.Length - 1, [], missing);
        }

        var dataRows = options.HasHeader ? records.Skip(1).Select((row, index) => (index + 2, row)) : records.Select((row, index) => (index + 1, row));
        return CreatePricePreview(fullPath, dataRows, columnMap, options);
    }

    /// <summary>
    /// Applies valid CSV price rows into a copied XML book.
    /// </summary>
    public async Task<GnuCashCsvPriceImportApplyResult> ApplyCsvPriceImportToCopiedBookAsync(
        string csvPath,
        GnuCashCsvPriceImportOptions options,
        string? workingBookPath = null,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewCsvPriceImportAsync(csvPath, options, cancellationToken).ConfigureAwait(false);
        var validRows = preview.Rows.Where(row => row.IsValid).ToArray();
        var workingPath = ResolveWorkingPath(workingBookPath, "csv-price-import");
        File.Copy(BookPath, workingPath, overwrite: false);
        var xml = GnuCashXmlBookDocument.Load(workingPath);
        var priceDb = EnsurePriceDatabase(xml);
        var priceIds = new List<string>();

        foreach (var row in validRows)
        {
            var priceId = GnuCashXmlBookDocument.GuidText();
            priceDb.Add(CreatePriceElement(
                priceId,
                new GnuCashPriceCreateRequest(
                    row.CommoditySpace!,
                    row.CommodityId!,
                    row.CurrencySpace!,
                    row.CurrencyId!,
                    row.Time!.Value,
                    row.Value!,
                    row.Source ?? "user:price",
                    row.Type ?? "last")));
            priceIds.Add(priceId);
        }

        xml.Save(workingPath);
        return new GnuCashCsvPriceImportApplyResult(
            validRows.Length > 0,
            BookPath,
            workingPath,
            preview,
            validRows.Length,
            priceIds,
            validRows.Length > 0
                ? "The copied XML book contains the imported prices."
                : "No valid CSV price rows were available to apply.");
    }

    private static GnuCashCsvPriceImportPreview CreatePricePreview(
        string fullPath,
        IEnumerable<(int RowNumber, IReadOnlyList<string> Values)> records,
        IReadOnlyDictionary<string, int> columnMap,
        GnuCashCsvPriceImportOptions options)
    {
        var rows = new List<GnuCashCsvPriceImportPreviewRow>();
        var issues = new List<GnuCashImportIssue>();
        var culture = ResolveCulture(options.CultureName);

        foreach (var record in records.Where(record => !record.Values.All(string.IsNullOrWhiteSpace)))
        {
            var rowIssues = new List<GnuCashImportIssue>();
            var time = ParsePriceDate(record, columnMap, options, rowIssues, culture);
            var value = ParsePriceAmount(record, columnMap, options, rowIssues, culture);
            var row = new GnuCashCsvPriceImportPreviewRow(
                record.RowNumber,
                RequiredText(record, columnMap, options.CommoditySpaceColumn, "MissingCommoditySpace", rowIssues),
                RequiredText(record, columnMap, options.CommodityIdColumn, "MissingCommodityId", rowIssues),
                RequiredText(record, columnMap, options.CurrencySpaceColumn, "MissingCurrencySpace", rowIssues),
                RequiredText(record, columnMap, options.CurrencyIdColumn, "MissingCurrencyId", rowIssues),
                time,
                value,
                OptionalText(record, columnMap, options.SourceColumn),
                OptionalText(record, columnMap, options.TypeColumn),
                rowIssues.Count == 0,
                rowIssues);
            rows.Add(row);
            issues.AddRange(rowIssues);
        }

        return new GnuCashCsvPriceImportPreview(fullPath, rows.Count, rows.Count(row => row.IsValid), rows.Count(row => !row.IsValid), rows, issues);
    }

    private static IReadOnlyDictionary<string, int> CreatePriceColumnMap(
        IReadOnlyList<IReadOnlyList<string>> records,
        GnuCashCsvPriceImportOptions options)
    {
        if (!options.HasHeader)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [options.CommoditySpaceColumn] = 0,
                [options.CommodityIdColumn] = 1,
                [options.CurrencySpaceColumn] = 2,
                [options.CurrencyIdColumn] = 3,
                [options.DateColumn] = 4,
                [options.ValueColumn] = 5
            };
        }

        return records[0]
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .Where(item => item.Header.Length > 0)
            .GroupBy(item => item.Header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<GnuCashImportIssue> ValidatePriceColumns(
        IReadOnlyDictionary<string, int> columnMap,
        GnuCashCsvPriceImportOptions options)
    {
        var issues = new List<GnuCashImportIssue>();
        foreach (var column in new[] { options.CommoditySpaceColumn, options.CommodityIdColumn, options.CurrencySpaceColumn, options.CurrencyIdColumn, options.DateColumn, options.ValueColumn })
        {
            if (!columnMap.ContainsKey(column))
            {
                issues.Add(new GnuCashImportIssue(1, "MissingColumn", $"The CSV price file is missing required column '{column}'."));
            }
        }

        return issues;
    }

    private static DateTimeOffset? ParsePriceDate((int RowNumber, IReadOnlyList<string> Values) record, IReadOnlyDictionary<string, int> columnMap, GnuCashCsvPriceImportOptions options, ICollection<GnuCashImportIssue> issues, CultureInfo culture)
    {
        var raw = ReadColumn(record.Values, columnMap, options.DateColumn);
        if (DateOnly.TryParseExact(raw, options.DateFormat, culture, DateTimeStyles.None, out var date))
        {
            return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        issues.Add(new GnuCashImportIssue(record.RowNumber, "InvalidDate", $"The price date '{raw}' does not match format '{options.DateFormat}'."));
        return null;
    }

    private static GnuCashAmount? ParsePriceAmount((int RowNumber, IReadOnlyList<string> Values) record, IReadOnlyDictionary<string, int> columnMap, GnuCashCsvPriceImportOptions options, ICollection<GnuCashImportIssue> issues, CultureInfo culture)
    {
        var raw = ReadColumn(record.Values, columnMap, options.ValueColumn);
        if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, culture, out var parsed))
        {
            var numerator = (long)decimal.Round(parsed * 100, 0, MidpointRounding.AwayFromZero);
            return new GnuCashAmount($"{numerator}/100", numerator, 100);
        }

        issues.Add(new GnuCashImportIssue(record.RowNumber, "InvalidValue", $"The price value '{raw}' is not a valid decimal value."));
        return null;
    }

    private static string? RequiredText((int RowNumber, IReadOnlyList<string> Values) record, IReadOnlyDictionary<string, int> columnMap, string column, string code, ICollection<GnuCashImportIssue> issues)
    {
        var value = ReadColumn(record.Values, columnMap, column)?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        issues.Add(new GnuCashImportIssue(record.RowNumber, code, $"Column '{column}' is required."));
        return null;
    }

    private static string? OptionalText((int RowNumber, IReadOnlyList<string> Values) record, IReadOnlyDictionary<string, int> columnMap, string? column) =>
        string.IsNullOrWhiteSpace(column) ? null : ReadColumn(record.Values, columnMap, column)?.Trim();

    private static string? ReadColumn(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> columnMap,
        string columnName) =>
        columnMap.TryGetValue(columnName, out var index) && index < values.Count
            ? values[index]
            : null;

    private static CultureInfo ResolveCulture(string? cultureName) =>
        string.IsNullOrWhiteSpace(cultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];
            if (current == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (current == ',' && !inQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
                continue;
            }

            value.Append(current);
        }

        values.Add(value.ToString());
        return values;
    }
}
