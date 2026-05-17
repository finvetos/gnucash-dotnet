namespace GnuCash.DotNet.Models;

/// <summary>
/// Request for creating a price in a copied XML book.
/// </summary>
public sealed record GnuCashPriceCreateRequest(
    string CommoditySpace,
    string CommodityId,
    string CurrencySpace,
    string CurrencyId,
    DateTimeOffset Time,
    GnuCashAmount Value,
    string Source = "user:price",
    string Type = "last",
    string? WorkingBookPath = null);

/// <summary>
/// Result of creating a price in a copied XML book and reopening it.
/// </summary>
public sealed record GnuCashPriceCreateResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    string PriceId,
    int BeforePriceCount,
    int AfterPriceCount,
    bool FoundAfterReopen,
    string Message);

/// <summary>
/// Request for marking split reconciliation state in a copied XML book.
/// </summary>
public sealed record GnuCashReconciliationWriteRequest(
    IReadOnlyList<string> SplitIds,
    string ReconciledState,
    DateTimeOffset? ReconciledAt = null,
    string? WorkingBookPath = null);

/// <summary>
/// Result of marking split reconciliation state in a copied XML book.
/// </summary>
public sealed record GnuCashReconciliationWriteResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    int RequestedSplitCount,
    int UpdatedSplitCount,
    IReadOnlyList<string> MissingSplitIds,
    string Message);
