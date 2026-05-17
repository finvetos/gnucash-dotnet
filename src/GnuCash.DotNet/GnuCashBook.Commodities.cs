using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Lists commodities and currencies that match a query.
    /// </summary>
    public async Task<IReadOnlyList<GnuCashCommodity>> ListCommoditiesAsync(
        GnuCashCommodityQuery? query,
        CancellationToken cancellationToken = default)
    {
        var commodities = await ListCommoditiesAsync(cancellationToken).ConfigureAwait(false);
        return commodities.Where(commodity => Matches(commodity, query)).ToArray();
    }

    /// <summary>
    /// Lists currency commodities in the book.
    /// </summary>
    public Task<IReadOnlyList<GnuCashCommodity>> ListCurrenciesAsync(
        CancellationToken cancellationToken = default) =>
        ListCommoditiesAsync(new GnuCashCommodityQuery(Space: "CURRENCY"), cancellationToken);

    /// <summary>
    /// Finds a commodity or currency by namespace and mnemonic.
    /// </summary>
    public async Task<GnuCashCommodity?> GetCommodityAsync(
        string space,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(space);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var commodities = await ListCommoditiesAsync(
            new GnuCashCommodityQuery(Space: space, Id: id),
            cancellationToken).ConfigureAwait(false);

        return commodities.SingleOrDefault();
    }
}
