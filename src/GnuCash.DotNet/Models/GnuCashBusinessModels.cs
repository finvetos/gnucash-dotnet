namespace GnuCash.DotNet.Models;

/// <summary>
/// Request for creating a customer through the native GnuCash engine in a copied book.
/// </summary>
public sealed record GnuCashCustomerCreateRequest(
    string CustomerId,
    string CustomerName,
    string CurrencySpace = "CURRENCY",
    string CurrencyId = "USD",
    string? WorkingBookPath = null);

/// <summary>
/// Result of creating a customer through the native GnuCash engine and reopening the copied book.
/// </summary>
public sealed record GnuCashCustomerCreateResult(
    bool IsReady,
    string SourceBookPath,
    string WorkingBookPath,
    string CustomerId,
    string CustomerName,
    string CurrencySpace,
    string CurrencyId,
    string? CreatedCustomerGuid,
    int? BeforeCustomerCount,
    int? AfterCustomerCount,
    bool FoundAfterReopen,
    int? BackendErrorCode,
    string? BackendErrorMessage,
    string Message);

/// <summary>
/// Customer business object in a GnuCash book.
/// </summary>
public sealed record GnuCashCustomer(
    string Id,
    string CustomerId,
    string Name,
    string? CurrencySpace,
    string? CurrencyId);
