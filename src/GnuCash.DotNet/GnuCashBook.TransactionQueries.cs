using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Finds a transaction by its GnuCash id.
    /// </summary>
    public async Task<GnuCashTransaction?> GetTransactionByIdAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        var transactions = await ListTransactionsAsync(cancellationToken).ConfigureAwait(false);

        return transactions.SingleOrDefault(transaction =>
            MatchesExact(transaction.Id, transactionId));
    }

    /// <summary>
    /// Lists split lines that match a transaction query.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashSplit>> ListSplitsAsync(
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var transactions = await ListTransactionsAsync(query, cancellationToken).ConfigureAwait(false);
        return transactions
            .SelectMany(transaction => transaction.Splits)
            .Where(split => Matches(split, query))
            .ToArray();
    }

    /// <summary>
    /// Finds a split by its GnuCash id.
    /// </summary>
    public async Task<GnuCashSplit?> GetSplitByIdAsync(
        string splitId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(splitId);
        var splits = await ListSplitsAsync(null, cancellationToken).ConfigureAwait(false);

        return splits.SingleOrDefault(split => MatchesExact(split.Id, splitId));
    }
}
