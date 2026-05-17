namespace GnuCash.DotNet.Models;

/// <summary>
/// Options for applying a CSV transaction import into a copied GnuCash book.
/// </summary>
public sealed record GnuCashCsvTransactionImportApplyOptions(
    GnuCashCsvTransactionImportOptions ImportOptions,
    string TransferAccountId,
    string CurrencySpace = "CURRENCY",
    string CurrencyId = "USD",
    bool SkipDuplicateRows = true,
    string? WorkingBookPath = null);

/// <summary>
/// Result of applying eligible CSV transaction rows into a copied GnuCash book.
/// </summary>
public sealed record GnuCashCsvTransactionImportApplyResult(
    bool IsReady,
    string SourceBookPath,
    string? WorkingBookPath,
    GnuCashTransactionImportAnalysis Analysis,
    int AppliedRows,
    int SkippedRows,
    IReadOnlyList<GnuCashCsvTransactionImportAppliedRow> Applied,
    IReadOnlyList<GnuCashCsvTransactionImportSkippedRow> Skipped,
    GnuCashTransactionBatchCreateResult? NativeResult,
    string Message);

/// <summary>
/// CSV row that produced a transaction in the copied book.
/// </summary>
public sealed record GnuCashCsvTransactionImportAppliedRow(
    int RowNumber,
    string Description,
    GnuCashAmount Value,
    string? CreatedTransactionGuid);

/// <summary>
/// CSV row that was not applied.
/// </summary>
public sealed record GnuCashCsvTransactionImportSkippedRow(
    int RowNumber,
    string Code,
    string Message);
