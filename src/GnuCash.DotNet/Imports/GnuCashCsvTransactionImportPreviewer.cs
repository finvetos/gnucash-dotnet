using System.Globalization;
using System.Text;
using GnuCash.DotNet.Models;

namespace GnuCash.DotNet.Imports;

internal sealed class GnuCashCsvTransactionImportPreviewer
{
    public async Task<GnuCashTransactionImportPreview> PreviewAsync(
        string csvPath,
        GnuCashCsvTransactionImportOptions options,
        int commodityFraction,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvPath);
        ArgumentNullException.ThrowIfNull(options);

        var fullPath = Path.GetFullPath(csvPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The CSV import file does not exist.", fullPath);
        }

        var lines = await File.ReadAllLinesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        if (lines.Length == 0)
        {
            return new GnuCashTransactionImportPreview(
                fullPath,
                options.AccountId,
                0,
                0,
                0,
                [],
                [new GnuCashImportIssue(1, "EmptyFile", "The CSV import file is empty.")]);
        }

        var records = lines.Select(ParseCsvLine).ToArray();
        return options.HasHeader
            ? PreviewWithHeader(fullPath, records, options, commodityFraction)
            : PreviewWithoutHeader(fullPath, records, options, commodityFraction);
    }

    private static GnuCashTransactionImportPreview PreviewWithHeader(
        string fullPath,
        IReadOnlyList<IReadOnlyList<string>> records,
        GnuCashCsvTransactionImportOptions options,
        int commodityFraction)
    {
        var headers = records[0];
        var columnMap = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Header))
            .GroupBy(item => item.Header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
        var missingColumnIssues = ValidateRequiredColumns(columnMap, options);
        if (missingColumnIssues.Count > 0)
        {
            return new GnuCashTransactionImportPreview(
                fullPath,
                options.AccountId,
                records.Count - 1,
                0,
                records.Count - 1,
                [],
                missingColumnIssues);
        }

        return PreviewRows(
            fullPath,
            records.Skip(1).Select((record, index) => new ImportRecord(index + 2, record)),
            options,
            commodityFraction,
            columnMap);
    }

    private static GnuCashTransactionImportPreview PreviewWithoutHeader(
        string fullPath,
        IReadOnlyList<IReadOnlyList<string>> records,
        GnuCashCsvTransactionImportOptions options,
        int commodityFraction)
    {
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [options.DateColumn] = 0,
            [options.DescriptionColumn] = 1,
            [options.AmountColumn] = 2
        };

        if (!string.IsNullOrWhiteSpace(options.NumberColumn))
        {
            columnMap[options.NumberColumn] = 3;
        }

        if (!string.IsNullOrWhiteSpace(options.MemoColumn))
        {
            columnMap[options.MemoColumn] = string.IsNullOrWhiteSpace(options.NumberColumn) ? 3 : 4;
        }

        return PreviewRows(
            fullPath,
            records.Select((record, index) => new ImportRecord(index + 1, record)),
            options,
            commodityFraction,
            columnMap);
    }

    private static GnuCashTransactionImportPreview PreviewRows(
        string fullPath,
        IEnumerable<ImportRecord> records,
        GnuCashCsvTransactionImportOptions options,
        int commodityFraction,
        IReadOnlyDictionary<string, int> columnMap)
    {
        var culture = ResolveCulture(options.CultureName);
        var rows = new List<GnuCashTransactionImportPreviewRow>();
        var issues = new List<GnuCashImportIssue>();

        foreach (var importRecord in records.Where(record => !IsBlank(record.Values)))
        {
            var rowIssues = new List<GnuCashImportIssue>();
            var postedDate = ParseDate(importRecord, options, columnMap, rowIssues, culture);
            var amount = ParseAmount(importRecord, options, columnMap, rowIssues, culture);
            var description = ReadColumn(importRecord.Values, columnMap, options.DescriptionColumn)?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                rowIssues.Add(new GnuCashImportIssue(
                    importRecord.RowNumber,
                    "MissingDescription",
                    "The transaction description is required."));
            }

            GnuCashAmount? value = null;
            if (amount is not null)
            {
                value = ToGnuCashAmount(amount.Value, commodityFraction);
            }

            var row = new GnuCashTransactionImportPreviewRow(
                importRecord.RowNumber,
                postedDate,
                ReadOptionalColumn(importRecord, columnMap, options.NumberColumn),
                description,
                ReadOptionalColumn(importRecord, columnMap, options.MemoColumn),
                amount,
                value,
                rowIssues.Count == 0,
                rowIssues);

            rows.Add(row);
            issues.AddRange(rowIssues);
        }

        return new GnuCashTransactionImportPreview(
            fullPath,
            options.AccountId,
            rows.Count,
            rows.Count(row => row.IsValid),
            rows.Count(row => !row.IsValid),
            rows,
            issues);
    }

    private static List<GnuCashImportIssue> ValidateRequiredColumns(
        IReadOnlyDictionary<string, int> columnMap,
        GnuCashCsvTransactionImportOptions options)
    {
        var issues = new List<GnuCashImportIssue>();
        AddMissingColumnIssue(issues, columnMap, options.DateColumn);
        AddMissingColumnIssue(issues, columnMap, options.DescriptionColumn);
        AddMissingColumnIssue(issues, columnMap, options.AmountColumn);

        return issues;
    }

    private static void AddMissingColumnIssue(
        ICollection<GnuCashImportIssue> issues,
        IReadOnlyDictionary<string, int> columnMap,
        string columnName)
    {
        if (!columnMap.ContainsKey(columnName))
        {
            issues.Add(new GnuCashImportIssue(
                1,
                "MissingColumn",
                $"The CSV import file is missing required column '{columnName}'."));
        }
    }

    private static DateOnly? ParseDate(
        ImportRecord record,
        GnuCashCsvTransactionImportOptions options,
        IReadOnlyDictionary<string, int> columnMap,
        ICollection<GnuCashImportIssue> issues,
        CultureInfo culture)
    {
        var raw = ReadColumn(record.Values, columnMap, options.DateColumn);
        if (string.IsNullOrWhiteSpace(raw))
        {
            issues.Add(new GnuCashImportIssue(record.RowNumber, "MissingDate", "The transaction date is required."));
            return null;
        }

        if (DateOnly.TryParseExact(raw.Trim(), options.DateFormat, culture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        issues.Add(new GnuCashImportIssue(
            record.RowNumber,
            "InvalidDate",
            $"The transaction date '{raw}' does not match format '{options.DateFormat}'."));
        return null;
    }

    private static decimal? ParseAmount(
        ImportRecord record,
        GnuCashCsvTransactionImportOptions options,
        IReadOnlyDictionary<string, int> columnMap,
        ICollection<GnuCashImportIssue> issues,
        CultureInfo culture)
    {
        var raw = ReadColumn(record.Values, columnMap, options.AmountColumn);
        if (string.IsNullOrWhiteSpace(raw))
        {
            issues.Add(new GnuCashImportIssue(record.RowNumber, "MissingAmount", "The transaction amount is required."));
            return null;
        }

        if (decimal.TryParse(
                raw.Trim(),
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                culture,
                out var parsed))
        {
            return parsed;
        }

        issues.Add(new GnuCashImportIssue(
            record.RowNumber,
            "InvalidAmount",
            $"The transaction amount '{raw}' is not a valid decimal value."));
        return null;
    }

    private static GnuCashAmount ToGnuCashAmount(decimal amount, int commodityFraction)
    {
        var denominator = commodityFraction <= 0 ? 100 : commodityFraction;
        var scaled = decimal.Round(amount * denominator, 0, MidpointRounding.AwayFromZero);
        if (scaled is > long.MaxValue or < long.MinValue)
        {
            throw new InvalidOperationException("The CSV import amount is too large for a GnuCash rational value.");
        }

        var numerator = (long)scaled;
        return new GnuCashAmount($"{numerator}/{denominator}", numerator, denominator);
    }

    private static string? ReadOptionalColumn(
        ImportRecord record,
        IReadOnlyDictionary<string, int> columnMap,
        string? columnName) =>
        string.IsNullOrWhiteSpace(columnName)
            ? null
            : ReadColumn(record.Values, columnMap, columnName)?.Trim();

    private static string? ReadColumn(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> columnMap,
        string columnName)
    {
        return columnMap.TryGetValue(columnName, out var index) && index < values.Count
            ? values[index]
            : null;
    }

    private static CultureInfo ResolveCulture(string? cultureName) =>
        string.IsNullOrWhiteSpace(cultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);

    private static bool IsBlank(IReadOnlyList<string> values) =>
        values.All(string.IsNullOrWhiteSpace);

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

    private sealed record ImportRecord(int RowNumber, IReadOnlyList<string> Values);
}
