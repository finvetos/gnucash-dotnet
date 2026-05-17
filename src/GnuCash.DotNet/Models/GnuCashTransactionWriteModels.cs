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
