using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists non-currency commodities, which usually represent securities.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashCommodity>> ListSecuritiesAsync(
        CancellationToken cancellationToken = default)
    {
        var commodities = await ListCommoditiesAsync(cancellationToken).ConfigureAwait(false);
        return commodities
            .Where(commodity => !MatchesExact(commodity.Space, "CURRENCY"))
            .ToArray();
    }

    /// <summary>
    /// Lists price database entries defined in the book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashPrice>> ListPricesAsync(
        CancellationToken cancellationToken = default) =>
        client.ListPricesAsync(BookPath, cancellationToken);

    /// <summary>
    /// Lists price database entries that match a query.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashPrice>> ListPricesAsync(
        GnuCashPriceQuery? query,
        CancellationToken cancellationToken = default)
    {
        var prices = await ListPricesAsync(cancellationToken).ConfigureAwait(false);
        return prices.Where(price => Matches(price, query)).ToArray();
    }

    /// <summary>
    /// Lists the latest price per commodity and currency pair.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashPrice>> ListLatestPricesAsync(
        GnuCashPriceQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var prices = await ListPricesAsync(query, cancellationToken).ConfigureAwait(false);

        return prices
            .GroupBy(price => new
            {
                price.CommoditySpace,
                price.CommodityId,
                price.CurrencySpace,
                price.CurrencyId
            })
            .Select(group => group
                .OrderByDescending(price => price.Time ?? DateTimeOffset.MinValue)
                .First())
            .ToArray();
    }

    /// <summary>
    /// Lists the latest price per commodity and currency pair.
    /// </summary>
    public Task<IReadOnlyList<GnuCashPrice>> ListLatestPricesAsync(
        CancellationToken cancellationToken) =>
        ListLatestPricesAsync(null, cancellationToken);

    /// <summary>
    /// Finds the latest price for one commodity and currency pair.
    /// </summary>
    public async Task<GnuCashPrice?> GetLatestPriceAsync(
        string commoditySpace,
        string commodityId,
        string currencySpace,
        string currencyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commoditySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(commodityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyId);

        var prices = await ListLatestPricesAsync(
            new GnuCashPriceQuery(
                CommoditySpace: commoditySpace,
                CommodityId: commodityId,
                CurrencySpace: currencySpace,
                CurrencyId: currencyId),
            cancellationToken).ConfigureAwait(false);

        return prices.SingleOrDefault();
    }
}
