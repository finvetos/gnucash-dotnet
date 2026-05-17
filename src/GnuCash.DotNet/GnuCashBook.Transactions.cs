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
        ValidateTransactionCreateRequest(request);

        return client.CreateTransactionInCopiedBookAsync(BookPath, request, cancellationToken);
    }

    /// <summary>
    /// Creates multiple balanced transactions in one copied book through the installed native GnuCash engine.
    /// </summary>
    public Task<GnuCashTransactionBatchCreateResult> CreateTransactionsInCopiedBookAsync(
        GnuCashTransactionBatchCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Transactions is null || request.Transactions.Count == 0)
        {
            throw new ArgumentException("At least one transaction is required.", nameof(request));
        }

        foreach (var transaction in request.Transactions)
        {
            ValidateTransactionCreateRequest(transaction);
            if (!string.IsNullOrWhiteSpace(transaction.WorkingBookPath))
            {
                throw new ArgumentException(
                    "Use the batch working book path instead of per-transaction working paths.",
                    nameof(request));
            }
        }

        return client.CreateTransactionsInCopiedBookAsync(BookPath, request, cancellationToken);
    }

    private static void ValidateTransactionCreateRequest(GnuCashTransactionCreateRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CurrencyId);

        if (request.Splits is null || request.Splits.Count < 2)
        {
            throw new ArgumentException("A transaction must contain at least two splits.", nameof(request));
        }
    }
}
