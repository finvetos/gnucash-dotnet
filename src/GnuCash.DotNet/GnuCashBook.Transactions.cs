using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Creates a balanced transaction in a copied book through the installed native GnuCash engine.
    /// </summary>
    public Task<GnuCashTransactionCreateResult> CreateTransactionInCopiedBookAsync(
        GnuCashTransactionCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencyId);

        if (request.Splits.Count < 2)
        {
            throw new ArgumentException("A transaction must contain at least two splits.", nameof(request));
        }

        return client.CreateTransactionInCopiedBookAsync(BookPath, request, cancellationToken);
    }
}
