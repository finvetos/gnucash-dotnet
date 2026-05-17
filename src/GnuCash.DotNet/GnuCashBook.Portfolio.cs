using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Creates a balance report grouped by commodity and currency.
    /// </summary>
    public async Task<GnuCashMultiCurrencyBalanceReport> CreateMultiCurrencyBalanceReportAsync(
        CancellationToken cancellationToken = default)
    {
        var balances = await ListAccountBalancesAsync(cancellationToken).ConfigureAwait(false);
        var grouped = balances
            .GroupBy(balance => new { balance.CommoditySpace, balance.CommodityId })
            .Select(group => new GnuCashCommodityBalance(
                group.Key.CommoditySpace,
                group.Key.CommodityId,
                Sum(group.Select(balance => balance.Balance)),
                group.Count()))
            .ToArray();

        return new GnuCashMultiCurrencyBalanceReport(
            BookPath,
            DateTimeOffset.UtcNow,
            grouped);
    }

    /// <summary>
    /// Creates a portfolio valuation report using security balances and latest prices.
    /// </summary>
    public async Task<GnuCashPortfolioValuationReport> CreatePortfolioValuationReportAsync(
        CancellationToken cancellationToken = default)
    {
        var securities = await ListSecuritiesAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var balances = await ListAccountBalancesAsync(cancellationToken).ConfigureAwait(false);
        var latestPrices = await ListLatestPricesAsync(cancellationToken).ConfigureAwait(false);
        var rows = securities
            .Select(security => CreatePortfolioRow(security, accounts, balances, latestPrices))
            .ToArray();

        return new GnuCashPortfolioValuationReport(
            BookPath,
            DateTimeOffset.UtcNow,
            rows);
    }

    private static GnuCashPortfolioValuationRow CreatePortfolioRow(
        GnuCashCommodity security,
        IReadOnlyList<GnuCashAccount> accounts,
        IReadOnlyList<GnuCashAccountBalance> balances,
        IReadOnlyList<GnuCashPrice> latestPrices)
    {
        var securityAccounts = accounts
            .Where(account =>
                MatchesExact(account.CommoditySpace, security.Space) &&
                MatchesExact(account.CommodityId, security.Id))
            .ToArray();
        var accountIds = securityAccounts
            .Select(account => account.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var quantity = Sum(balances
            .Where(balance => accountIds.Contains(balance.AccountId))
            .Select(balance => balance.Balance));
        var latestPrice = latestPrices
            .Where(price =>
                MatchesExact(price.CommoditySpace, security.Space) &&
                MatchesExact(price.CommodityId, security.Id))
            .OrderByDescending(price => price.Time ?? DateTimeOffset.MinValue)
            .FirstOrDefault();
        var marketValue = quantity.DecimalValue is not null && latestPrice?.Value.DecimalValue is not null
            ? (decimal?)(quantity.DecimalValue.Value * latestPrice.Value.DecimalValue.Value)
            : null;

        return new GnuCashPortfolioValuationRow(
            security.Space,
            security.Id,
            security.Name,
            latestPrice?.CurrencySpace,
            latestPrice?.CurrencyId,
            quantity,
            latestPrice,
            marketValue,
            securityAccounts.Length);
    }
}
