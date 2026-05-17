namespace GnuCash.DotNet.Models;

/// <summary>
/// Request for creating a balanced transaction through the native engine in a copied book.
/// </summary>
public sealed record GnuCashTransactionCreateRequest(
    string Description,
    DateTimeOffset PostedAt,
    IReadOnlyList<GnuCashTransactionSplitCreateRequest> Splits,
    string CurrencySpace = "CURRENCY",
    string CurrencyId = "USD",
    string? Number = null,
    string? WorkingBookPath = null);

/// <summary>
/// Split request for native transaction creation.
/// </summary>
public sealed record GnuCashTransactionSplitCreateRequest(
    string AccountId,
    GnuCashAmount Value,
    GnuCashAmount? Quantity = null,
    string? Memo = null,
    string? Action = null);

/// <summary>
/// Result of creating a transaction through the native engine and reopening the copied book.
/// </summary>
public sealed record GnuCashTransactionCreateResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    string Description,
    string CurrencySpace,
    string CurrencyId,
    string? Number,
    string? CreatedTransactionGuid,
    int SplitCount,
    int? BeforeTransactionCount,
    int? AfterTransactionCount,
    bool FoundAfterReopen,
    int? BackendErrorCode,
    string? BackendErrorMessage,
    string Message);

/// <summary>
/// Request for creating multiple balanced transactions through the native engine in one copied book.
/// </summary>
public sealed record GnuCashTransactionBatchCreateRequest(
    IReadOnlyList<GnuCashTransactionCreateRequest> Transactions,
    string? WorkingBookPath = null);

/// <summary>
/// Result of creating multiple transactions through the native engine and reopening the copied book.
/// </summary>
public sealed record GnuCashTransactionBatchCreateResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    int RequestedTransactionCount,
    int CreatedTransactionCount,
    int SplitCount,
    IReadOnlyList<GnuCashCreatedTransactionResult> CreatedTransactions,
    int? BeforeTransactionCount,
    int? AfterTransactionCount,
    int FoundAfterReopenCount,
    int? BackendErrorCode,
    string? BackendErrorMessage,
    string Message);

/// <summary>
/// Per-transaction verification row from a native batch create operation.
/// </summary>
public sealed record GnuCashCreatedTransactionResult(
    int Index,
    string Description,
    string? Number,
    string? CreatedTransactionGuid,
    int SplitCount,
    bool FoundAfterReopen);
