using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists customer business objects through the installed native GnuCash engine.
    /// </summary>
    public Task<IReadOnlyList<GnuCashCustomer>> ListCustomersAsync(
        CancellationToken cancellationToken = default) =>
        client.ListCustomersAsync(BookPath, cancellationToken);

    /// <summary>
    /// Creates a customer in a copied book through the installed native GnuCash engine.
    /// </summary>
    public Task<GnuCashCustomerCreateResult> CreateCustomerInCopiedBookAsync(
        GnuCashCustomerCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CustomerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CustomerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencyId);

        return client.CreateCustomerInCopiedBookAsync(BookPath, request, cancellationToken);
    }
}
