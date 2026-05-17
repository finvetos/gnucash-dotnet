namespace GnuCash.DotNet.Models;

/// <summary>
/// Filters commodities and currencies in an opened book.
/// </summary>
public sealed record GnuCashCommodityQuery(
    string? Space = null,
    string? Id = null,
    string? NameContains = null,
    string? XCode = null);

/// <summary>
/// Full account path from the root account to a child account.
/// </summary>
public sealed record GnuCashAccountPath(
    string AccountId,
    string FullName,
    IReadOnlyList<string> AccountIds,
    IReadOnlyList<string> AccountNames);
