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

    /// <summary>
    /// Previews a CSV transaction import and finds likely duplicates in the opened book.
    /// </summary>
    public async Task<GnuCashTransactionImportAnalysis> AnalyzeCsvTransactionImportAsync(
        string csvPath,
        GnuCashCsvTransactionImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AccountId);

        var preview = await PreviewCsvTransactionImportAsync(csvPath, options, cancellationToken).ConfigureAwait(false);
        var transactions = await ListTransactionsAsync(
            new GnuCashTransactionQuery(AccountId: options.AccountId),
            cancellationToken).ConfigureAwait(false);
        var matches = preview.Rows
            .Where(row => row.IsValid)
            .SelectMany(row => FindImportMatches(row, transactions, options.AccountId))
            .ToArray();
        var duplicateRows = matches
            .Select(match => match.RowNumber)
            .Distinct()
            .Count();

        return new GnuCashTransactionImportAnalysis(
            preview,
            matches,
            duplicateRows,
            preview.ValidRows - duplicateRows,
            preview.InvalidRows + duplicateRows);
    }

    private static IEnumerable<GnuCashTransactionImportMatch> FindImportMatches(
        GnuCashTransactionImportPreviewRow row,
        IEnumerable<GnuCashTransaction> transactions,
        string accountId)
    {
        foreach (var transaction in transactions)
        {
            if (!MatchesImportDate(row.PostedDate, transaction.PostedAt) ||
                !MatchesImportText(row.Description, transaction.Description))
            {
                continue;
            }

            foreach (var split in transaction.Splits.Where(split =>
                         MatchesExact(split.AccountId, accountId) &&
                         AreEquivalentAmounts(row.Value, split.Value)))
            {
                yield return new GnuCashTransactionImportMatch(
                    row.RowNumber,
                    transaction.Id,
                    split.Id,
                    split.AccountId,
                    transaction.PostedAt,
                    transaction.Description,
                    split.Value,
                    "SameAccountDateAmountDescription",
                    "An existing transaction matches the account, posted date, amount, and description.");
            }
        }
    }

    private static bool MatchesImportDate(DateOnly? importDate, DateTimeOffset? postedAt) =>
        importDate is not null &&
        postedAt is not null &&
        importDate == DateOnly.FromDateTime(postedAt.Value.Date);

    private static bool MatchesImportText(string? importText, string? existingText) =>
        string.Equals(
            NormalizeImportText(importText),
            NormalizeImportText(existingText),
            StringComparison.OrdinalIgnoreCase);

    private static string NormalizeImportText(string? value) =>
        string.Join(
            ' ',
            (value ?? string.Empty).Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
