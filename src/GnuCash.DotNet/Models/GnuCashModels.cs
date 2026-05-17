namespace GnuCash.DotNet.Models;

/// <summary>
/// Summary of an opened GnuCash book.
/// </summary>
public sealed record GnuCashBookInfo(
    string BookPath,
    string FileFormat,
    string? BookId,
    int CommodityCount,
    int AccountCount,
    int TransactionCount,
    int SplitCount,
    int PriceCount);

/// <summary>
/// Commodity or currency definition in a GnuCash book.
/// </summary>
public sealed record GnuCashCommodity(
    string Space,
    string Id,
    string? Name,
    string? XCode,
    int Fraction);

/// <summary>
/// Price database entry for a commodity/security.
/// </summary>
public sealed record GnuCashPrice(
    string Id,
    string CommoditySpace,
    string CommodityId,
    string CurrencySpace,
    string CurrencyId,
    DateTimeOffset? Time,
    string? Source,
    string? Type,
    GnuCashAmount Value);

/// <summary>
/// Filters price database entries.
/// </summary>
public sealed record GnuCashPriceQuery(
    string? CommoditySpace = null,
    string? CommodityId = null,
    string? CurrencySpace = null,
    string? CurrencyId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Source = null,
    string? Type = null);

/// <summary>
/// Account definition in a GnuCash book.
/// </summary>
public sealed record GnuCashAccount(
    string Id,
    string Name,
    string Type,
    string? ParentId,
    string? CommoditySpace,
    string? CommodityId,
    string? Code,
    string? Description,
    bool IsPlaceholder);

/// <summary>
/// Filters accounts in an opened book.
/// </summary>
public sealed record GnuCashAccountQuery(
    string? Id = null,
    string? NameContains = null,
    string? Type = null,
    string? ParentId = null,
    string? CommoditySpace = null,
    string? CommodityId = null);

/// <summary>
/// Rational amount from a GnuCash book, preserving the source fraction.
/// </summary>
public sealed record GnuCashAmount(
    string RawValue,
    long? Numerator,
    long? Denominator)
{
    /// <summary>
    /// Decimal projection of the rational value when both parts are available.
    /// </summary>
    public decimal? DecimalValue =>
        Numerator is not null && Denominator is not null and not 0
            ? Numerator.Value / (decimal)Denominator.Value
            : null;
}

/// <summary>
/// Split line inside a GnuCash transaction.
/// </summary>
public sealed record GnuCashSplit(
    string Id,
    string AccountId,
    string? Memo,
    string? Action,
    string ReconciledState,
    GnuCashAmount Value,
    GnuCashAmount Quantity,
    DateTimeOffset? ReconciledAt);

/// <summary>
/// Transaction and its split lines.
/// </summary>
public sealed record GnuCashTransaction(
    string Id,
    string? Number,
    string? Description,
    string? CurrencySpace,
    string? CurrencyId,
    DateTimeOffset? PostedAt,
    DateTimeOffset? EnteredAt,
    IReadOnlyList<GnuCashSplit> Splits);

/// <summary>
/// Filters transactions in an opened book.
/// </summary>
public sealed record GnuCashTransactionQuery(
    string? AccountId = null,
    DateTimeOffset? PostedFrom = null,
    DateTimeOffset? PostedTo = null,
    string? Number = null,
    string? DescriptionContains = null,
    string? CurrencySpace = null,
    string? CurrencyId = null,
    string? ReconciledState = null);

/// <summary>
/// Balance snapshot for a single account.
/// </summary>
public sealed record GnuCashAccountBalance(
    string AccountId,
    string AccountName,
    string? CommoditySpace,
    string? CommodityId,
    GnuCashAmount Balance,
    int SplitCount);

/// <summary>
/// Account summary report for an opened book.
/// </summary>
public sealed record GnuCashAccountSummaryReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<GnuCashAccountSummaryRow> Rows,
    IReadOnlyList<GnuCashAccountGroupTotal> Totals);

/// <summary>
/// Row in an account summary report.
/// </summary>
public sealed record GnuCashAccountSummaryRow(
    string AccountId,
    string AccountName,
    string AccountType,
    string? ParentId,
    string? CommoditySpace,
    string? CommodityId,
    bool IsPlaceholder,
    GnuCashAmount Balance,
    int SplitCount);

/// <summary>
/// Total for accounts grouped by type and commodity.
/// </summary>
public sealed record GnuCashAccountGroupTotal(
    string AccountType,
    string? CommoditySpace,
    string? CommodityId,
    GnuCashAmount Balance,
    int AccountCount);

/// <summary>
/// Transaction report for an opened book.
/// </summary>
public sealed record GnuCashTransactionReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    GnuCashTransactionQuery? Query,
    IReadOnlyList<GnuCashTransactionReportRow> Rows);

/// <summary>
/// Split-level row in a transaction report.
/// </summary>
public sealed record GnuCashTransactionReportRow(
    string TransactionId,
    DateTimeOffset? PostedAt,
    string? Number,
    string? Description,
    string? CurrencySpace,
    string? CurrencyId,
    string SplitId,
    string AccountId,
    string? AccountName,
    string ReconciledState,
    string? Memo,
    string? Action,
    GnuCashAmount Value,
    GnuCashAmount Quantity);

/// <summary>
/// Reconciliation state snapshot for a single account.
/// </summary>
public sealed record GnuCashReconciliationSummary(
    string AccountId,
    string AccountName,
    string? CommoditySpace,
    string? CommodityId,
    int UnreconciledSplitCount,
    int ClearedSplitCount,
    int ReconciledSplitCount,
    int OtherSplitCount,
    GnuCashAmount UnreconciledBalance,
    GnuCashAmount ClearedBalance,
    GnuCashAmount ReconciledBalance,
    GnuCashAmount OtherBalance);

/// <summary>
/// Options for previewing a CSV transaction import.
/// </summary>
public sealed record GnuCashCsvTransactionImportOptions(
    string AccountId,
    string DateColumn = "Date",
    string DescriptionColumn = "Description",
    string AmountColumn = "Amount",
    string? NumberColumn = null,
    string? MemoColumn = null,
    string DateFormat = "yyyy-MM-dd",
    string? CultureName = null,
    bool HasHeader = true);

/// <summary>
/// Preview result for a CSV transaction import.
/// </summary>
public sealed record GnuCashTransactionImportPreview(
    string SourcePath,
    string AccountId,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<GnuCashTransactionImportPreviewRow> Rows,
    IReadOnlyList<GnuCashImportIssue> Issues);

/// <summary>
/// Preview row for a CSV transaction import.
/// </summary>
public sealed record GnuCashTransactionImportPreviewRow(
    int RowNumber,
    DateOnly? PostedDate,
    string? Number,
    string? Description,
    string? Memo,
    decimal? Amount,
    GnuCashAmount? Value,
    bool IsValid,
    IReadOnlyList<GnuCashImportIssue> Issues);

/// <summary>
/// Issue found while previewing an import.
/// </summary>
public sealed record GnuCashImportIssue(
    int RowNumber,
    string Code,
    string Message);
