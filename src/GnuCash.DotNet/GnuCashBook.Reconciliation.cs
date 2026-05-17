using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists reconciliation state summaries by account.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashReconciliationSummary>> ListReconciliationSummariesAsync(
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var accountQuery = string.IsNullOrWhiteSpace(query?.AccountId)
            ? null
            : new GnuCashAccountQuery(Id: query.AccountId);
        var accounts = await ListAccountsAsync(accountQuery, cancellationToken).ConfigureAwait(false);
        var transactions = await ListTransactionsAsync(query, cancellationToken).ConfigureAwait(false);

        return accounts
            .Select(account => CreateReconciliationSummary(account, transactions, query))
            .ToArray();
    }

    /// <summary>
    /// Lists reconciliation state summaries by account.
    /// </summary>
    public Task<IReadOnlyList<GnuCashReconciliationSummary>> ListReconciliationSummariesAsync(
        CancellationToken cancellationToken) =>
        ListReconciliationSummariesAsync(null, cancellationToken);

    /// <summary>
    /// Gets the reconciliation state summary for one account.
    /// </summary>
    public async Task<GnuCashReconciliationSummary?> GetReconciliationSummaryAsync(
        string accountId,
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        var scopedQuery = query is null
            ? new GnuCashTransactionQuery(AccountId: accountId)
            : query with { AccountId = accountId };
        var summaries = await ListReconciliationSummariesAsync(scopedQuery, cancellationToken).ConfigureAwait(false);

        return summaries.SingleOrDefault();
    }

    /// <summary>
    /// Gets the reconciliation state summary for one account.
    /// </summary>
    public Task<GnuCashReconciliationSummary?> GetReconciliationSummaryAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        GetReconciliationSummaryAsync(accountId, null, cancellationToken);

    private static GnuCashReconciliationSummary CreateReconciliationSummary(
        GnuCashAccount account,
        IReadOnlyList<GnuCashTransaction> transactions,
        GnuCashTransactionQuery? query)
    {
        var splits = transactions
            .SelectMany(transaction => transaction.Splits)
            .Where(split => MatchesExact(split.AccountId, account.Id))
            .Where(split => Matches(split, query))
            .ToArray();
        var unreconciled = splits
            .Where(split => MatchesExact(split.ReconciledState, "n"))
            .ToArray();
        var cleared = splits
            .Where(split => MatchesExact(split.ReconciledState, "c"))
            .ToArray();
        var reconciled = splits
            .Where(split => MatchesExact(split.ReconciledState, "y"))
            .ToArray();
        var other = splits
            .Where(split =>
                !MatchesExact(split.ReconciledState, "n") &&
                !MatchesExact(split.ReconciledState, "c") &&
                !MatchesExact(split.ReconciledState, "y"))
            .ToArray();

        return new GnuCashReconciliationSummary(
            account.Id,
            account.Name,
            account.CommoditySpace,
            account.CommodityId,
            unreconciled.Length,
            cleared.Length,
            reconciled.Length,
            other.Length,
            Sum(unreconciled.Select(split => split.Value)),
            Sum(cleared.Select(split => split.Value)),
            Sum(reconciled.Select(split => split.Value)),
            Sum(other.Select(split => split.Value)));
    }
}
