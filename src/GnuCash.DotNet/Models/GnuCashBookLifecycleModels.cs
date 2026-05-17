namespace GnuCash.DotNet.Models;

/// <summary>
/// Options for creating a new XML GnuCash book.
/// </summary>
public sealed record GnuCashBookCreateOptions(
    string CurrencySpace = "CURRENCY",
    string CurrencyId = "USD",
    string CurrencyName = "US Dollar",
    int CurrencyFraction = 100,
    string RootAccountName = "Root Account");

/// <summary>
/// Request for creating an account in a copied XML book.
/// </summary>
public sealed record GnuCashAccountCreateRequest(
    string Name,
    string Type,
    string ParentId,
    string CommoditySpace = "CURRENCY",
    string CommodityId = "USD",
    string? Code = null,
    string? Description = null,
    bool IsPlaceholder = false,
    string? WorkingBookPath = null);

/// <summary>
/// Result of creating an account in a copied XML book and reopening it.
/// </summary>
public sealed record GnuCashAccountCreateResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    string AccountId,
    int BeforeAccountCount,
    int AfterAccountCount,
    bool FoundAfterReopen,
    string Message);
