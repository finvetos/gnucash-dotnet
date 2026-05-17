using System.Globalization;
using System.Text;
using System.Text.Json;
using GnuCash.DotNet.Models;

namespace GnuCash.DotNet.Reports;

/// <summary>
/// Serializes structured report DTOs to common interchange formats.
/// </summary>
public static class GnuCashReportSerializer
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes a report DTO to JSON.
    /// </summary>
    public static string ToJson<TReport>(TReport report, bool indented = true) =>
        JsonSerializer.Serialize(report, indented ? IndentedJson : null);

    /// <summary>
    /// Serializes an account summary report to CSV.
    /// </summary>
    public static string ToCsv(GnuCashAccountSummaryReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        AppendCsvLine(
            builder,
            "AccountId",
            "AccountName",
            "AccountType",
            "ParentId",
            "CommoditySpace",
            "CommodityId",
            "IsPlaceholder",
            "BalanceRaw",
            "BalanceDecimal",
            "SplitCount");

        foreach (var row in report.Rows)
        {
            AppendCsvLine(
                builder,
                row.AccountId,
                row.AccountName,
                row.AccountType,
                row.ParentId,
                row.CommoditySpace,
                row.CommodityId,
                row.IsPlaceholder.ToString(CultureInfo.InvariantCulture),
                row.Balance.RawValue,
                FormatDecimal(row.Balance.DecimalValue),
                row.SplitCount.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Serializes a transaction report to CSV.
    /// </summary>
    public static string ToCsv(GnuCashTransactionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        AppendCsvLine(
            builder,
            "TransactionId",
            "PostedAt",
            "Number",
            "Description",
            "CurrencySpace",
            "CurrencyId",
            "SplitId",
            "AccountId",
            "AccountName",
            "ReconciledState",
            "Memo",
            "Action",
            "ValueRaw",
            "ValueDecimal",
            "QuantityRaw",
            "QuantityDecimal");

        foreach (var row in report.Rows)
        {
            AppendCsvLine(
                builder,
                row.TransactionId,
                row.PostedAt?.ToString("O", CultureInfo.InvariantCulture),
                row.Number,
                row.Description,
                row.CurrencySpace,
                row.CurrencyId,
                row.SplitId,
                row.AccountId,
                row.AccountName,
                row.ReconciledState,
                row.Memo,
                row.Action,
                row.Value.RawValue,
                FormatDecimal(row.Value.DecimalValue),
                row.Quantity.RawValue,
                FormatDecimal(row.Quantity.DecimalValue));
        }

        return builder.ToString();
    }

    private static void AppendCsvLine(StringBuilder builder, params string?[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsv(values[index]));
        }

        builder.AppendLine();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }

    private static string? FormatDecimal(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture);
}
