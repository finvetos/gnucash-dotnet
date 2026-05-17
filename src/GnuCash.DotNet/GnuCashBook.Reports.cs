using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Creates a structured account summary report.
    /// </summary>
    public async Task<GnuCashAccountSummaryReport> CreateAccountSummaryReportAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var balances = await ListAccountBalancesAsync(cancellationToken).ConfigureAwait(false);
        var accountsById = accounts.ToDictionary(account => account.Id, StringComparer.OrdinalIgnoreCase);
        var rows = balances
            .Select(balance =>
            {
                var account = accountsById[balance.AccountId];
                return new GnuCashAccountSummaryRow(
                    account.Id,
                    account.Name,
                    account.Type,
                    account.ParentId,
                    account.CommoditySpace,
                    account.CommodityId,
                    account.IsPlaceholder,
                    balance.Balance,
                    balance.SplitCount);
            })
            .ToArray();
        var totals = rows
            .GroupBy(row => new { row.AccountType, row.CommoditySpace, row.CommodityId })
            .Select(group => new GnuCashAccountGroupTotal(
                group.Key.AccountType,
                group.Key.CommoditySpace,
                group.Key.CommodityId,
                Sum(group.Select(row => row.Balance)),
                group.Count()))
            .ToArray();

        return new GnuCashAccountSummaryReport(BookPath, DateTimeOffset.UtcNow, rows, totals);
    }

    /// <summary>
    /// Creates a structured split-level transaction report.
    /// </summary>
    public async Task<GnuCashTransactionReport> CreateTransactionReportAsync(
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var accountsById = accounts.ToDictionary(account => account.Id, StringComparer.OrdinalIgnoreCase);
        var transactions = await ListTransactionsAsync(query, cancellationToken).ConfigureAwait(false);
        var rows = transactions
            .SelectMany(transaction => transaction.Splits
                .Where(split => Matches(split, query))
                .Select(split => CreateTransactionReportRow(transaction, split, accountsById)))
            .ToArray();

        return new GnuCashTransactionReport(BookPath, DateTimeOffset.UtcNow, query, rows);
    }

    /// <summary>
    /// Creates a structured split-level transaction report.
    /// </summary>
    public Task<GnuCashTransactionReport> CreateTransactionReportAsync(
        CancellationToken cancellationToken) =>
        CreateTransactionReportAsync(null, cancellationToken);

    private static GnuCashTransactionReportRow CreateTransactionReportRow(
        GnuCashTransaction transaction,
        GnuCashSplit split,
        IReadOnlyDictionary<string, GnuCashAccount> accountsById)
    {
        accountsById.TryGetValue(split.AccountId, out var account);

        return new GnuCashTransactionReportRow(
            transaction.Id,
            transaction.PostedAt,
            transaction.Number,
            transaction.Description,
            transaction.CurrencySpace,
            transaction.CurrencyId,
            split.Id,
            split.AccountId,
            account?.Name,
            split.ReconciledState,
            split.Memo,
            split.Action,
            split.Value,
            split.Quantity);
    }
}
