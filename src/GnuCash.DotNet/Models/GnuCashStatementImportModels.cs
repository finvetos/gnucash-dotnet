namespace GnuCash.DotNet.Models;

/// <summary>
/// Preview of an OFX or QFX statement import.
/// </summary>
public sealed record GnuCashOfxStatementPreview(
    string SourcePath,
    int TransactionCount,
    IReadOnlyList<GnuCashStatementPreviewTransaction> Transactions);

/// <summary>
/// Preview of a QIF statement import.
/// </summary>
public sealed record GnuCashQifStatementPreview(
    string SourcePath,
    string? AccountType,
    int TransactionCount,
    IReadOnlyList<GnuCashStatementPreviewTransaction> Transactions);

/// <summary>
/// Statement transaction parsed from an import file.
/// </summary>
public sealed record GnuCashStatementPreviewTransaction(
    string? ExternalId,
    DateOnly? PostedDate,
    string? Number,
    string? Payee,
    string? Memo,
    decimal? Amount);

/// <summary>
/// Options for CSV price import preview and copied-book apply.
/// </summary>
public sealed record GnuCashCsvPriceImportOptions(
    string CommoditySpaceColumn = "CommoditySpace",
    string CommodityIdColumn = "CommodityId",
    string CurrencySpaceColumn = "CurrencySpace",
    string CurrencyIdColumn = "CurrencyId",
    string DateColumn = "Date",
    string ValueColumn = "Value",
    string DateFormat = "yyyy-MM-dd",
    string? SourceColumn = null,
    string? TypeColumn = null,
    string? CultureName = null,
    bool HasHeader = true);

/// <summary>
/// Preview result for CSV price import.
/// </summary>
public sealed record GnuCashCsvPriceImportPreview(
    string SourcePath,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<GnuCashCsvPriceImportPreviewRow> Rows,
    IReadOnlyList<GnuCashImportIssue> Issues);

/// <summary>
/// Parsed CSV price import row.
/// </summary>
public sealed record GnuCashCsvPriceImportPreviewRow(
    int RowNumber,
    string? CommoditySpace,
    string? CommodityId,
    string? CurrencySpace,
    string? CurrencyId,
    DateTimeOffset? Time,
    GnuCashAmount? Value,
    string? Source,
    string? Type,
    bool IsValid,
    IReadOnlyList<GnuCashImportIssue> Issues);

/// <summary>
/// Result of applying CSV prices into a copied XML book.
/// </summary>
public sealed record GnuCashCsvPriceImportApplyResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    GnuCashCsvPriceImportPreview Preview,
    int AppliedRowCount,
    IReadOnlyList<string> CreatedPriceIds,
    string Message);
