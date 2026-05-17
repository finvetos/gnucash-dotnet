using GnuCash.DotNet.Imports;
using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Previews a CSV transaction import without changing the book.
    /// </summary>
    public async Task<GnuCashTransactionImportPreview> PreviewCsvTransactionImportAsync(
        string csvPath,
        GnuCashCsvTransactionImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AccountId);

        var account = await GetAccountByIdAsync(options.AccountId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            throw new InvalidOperationException(
                $"Cannot preview import because account '{options.AccountId}' does not exist in this book.");
        }

        var commodities = await ListCommoditiesAsync(cancellationToken).ConfigureAwait(false);
        var fraction = commodities
            .FirstOrDefault(commodity =>
                MatchesExact(commodity.Space, account.CommoditySpace) &&
                MatchesExact(commodity.Id, account.CommodityId))
            ?.Fraction ?? 100;

        return await new GnuCashCsvTransactionImportPreviewer()
            .PreviewAsync(csvPath, options, fraction, cancellationToken)
            .ConfigureAwait(false);
    }
}
