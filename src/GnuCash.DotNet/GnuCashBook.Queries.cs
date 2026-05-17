using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists accounts that match a query.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashAccount>> ListAccountsAsync(
        GnuCashAccountQuery? query,
        CancellationToken cancellationToken = default)
    {
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        return accounts.Where(account => Matches(account, query)).ToArray();
    }

    /// <summary>
    /// Finds an account by its GnuCash id.
    /// </summary>
    public async Task<GnuCashAccount?> GetAccountByIdAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        var accounts = await ListAccountsAsync(
            new GnuCashAccountQuery(Id: accountId),
            cancellationToken).ConfigureAwait(false);

        return accounts.SingleOrDefault();
    }

    /// <summary>
    /// Lists transactions that match a query.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashTransaction>> ListTransactionsAsync(
        GnuCashTransactionQuery? query,
        CancellationToken cancellationToken = default)
    {
        var transactions = await ListTransactionsAsync(cancellationToken).ConfigureAwait(false);
        return transactions.Where(transaction => Matches(transaction, query)).ToArray();
    }

    /// <summary>
    /// Calculates balances for accounts in the book.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashAccountBalance>> ListAccountBalancesAsync(
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var accountQuery = string.IsNullOrWhiteSpace(query?.AccountId)
            ? null
            : new GnuCashAccountQuery(Id: query.AccountId);
        var accounts = await ListAccountsAsync(accountQuery, cancellationToken).ConfigureAwait(false);
        var transactions = await ListTransactionsAsync(query, cancellationToken).ConfigureAwait(false);

        return accounts
            .Select(account => CreateBalance(account, transactions, query))
            .ToArray();
    }

    /// <summary>
    /// Calculates balances for accounts in the book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashAccountBalance>> ListAccountBalancesAsync(
        CancellationToken cancellationToken) =>
        ListAccountBalancesAsync(null, cancellationToken);

    /// <summary>
    /// Calculates a balance for a single account.
    /// </summary>
    public async Task<GnuCashAccountBalance?> GetAccountBalanceAsync(
        string accountId,
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        var scopedQuery = query is null
            ? new GnuCashTransactionQuery(AccountId: accountId)
            : query with { AccountId = accountId };
        var balances = await ListAccountBalancesAsync(scopedQuery, cancellationToken).ConfigureAwait(false);

        return balances.SingleOrDefault();
    }

    /// <summary>
    /// Calculates a balance for a single account.
    /// </summary>
    public Task<GnuCashAccountBalance?> GetAccountBalanceAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        GetAccountBalanceAsync(accountId, null, cancellationToken);
}
