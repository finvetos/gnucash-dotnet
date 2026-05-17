namespace GnuCash.DotNet.Models;

/// <summary>
/// Reconciliation balance preview for an account before writing reconciliation state changes.
/// </summary>
public sealed record GnuCashReconciliationPreview(
    string AccountId,
    string AccountName,
    string? CommoditySpace,
    string? CommodityId,
    GnuCashAmount ExpectedEndingBalance,
    GnuCashAmount ActualEndingBalance,
    GnuCashAmount Variance,
    bool IsBalanced,
    GnuCashReconciliationSummary Summary);
