namespace GnuCash.DotNet.Models;

/// <summary>
/// Lot/cost-basis object read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashLot(
    string Id,
    string? Title,
    string? Notes,
    int SplitCount);

/// <summary>
/// Vendor business object read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashVendor(
    string Id,
    string? VendorId,
    string? Name,
    string? CurrencySpace,
    string? CurrencyId);

/// <summary>
/// Invoice or bill object read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashBusinessDocument(
    string Id,
    string? DocumentId,
    string? Type,
    string? OwnerId,
    string? CurrencySpace,
    string? CurrencyId,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? PostedAt,
    int EntryCount);

/// <summary>
/// Payment term read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashBillTerm(
    string Id,
    string? Name,
    string? Description);

/// <summary>
/// Tax table read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashTaxTable(
    string Id,
    string? Name,
    int EntryCount);

/// <summary>
/// Scheduled transaction definition read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashScheduledTransaction(
    string Id,
    string? Name,
    string? Enabled,
    string? Frequency);

/// <summary>
/// Budget definition read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashBudget(
    string Id,
    string? Name,
    string? Description,
    int AmountCount);

/// <summary>
/// Slot or custom metadata value read from a GnuCash XML book.
/// </summary>
public sealed record GnuCashSlotValue(
    string OwnerId,
    string Key,
    string? ValueType,
    string? Value);
