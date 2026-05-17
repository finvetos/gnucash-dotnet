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

    /// <summary>
    /// Applies valid non-duplicate CSV transaction rows into a copied book through the native GnuCash engine.
    /// </summary>
    public async Task<GnuCashCsvTransactionImportApplyResult> ApplyCsvTransactionImportToCopiedBookAsync(
        string csvPath,
        GnuCashCsvTransactionImportApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.ImportOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.TransferAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CurrencySpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CurrencyId);

        var transferAccount = await GetAccountByIdAsync(options.TransferAccountId, cancellationToken).ConfigureAwait(false);
        if (transferAccount is null)
        {
            throw new InvalidOperationException(
                $"Cannot apply import because transfer account '{options.TransferAccountId}' does not exist in this book.");
        }

        var analysis = await AnalyzeCsvTransactionImportAsync(
            csvPath,
            options.ImportOptions,
            cancellationToken).ConfigureAwait(false);
        var duplicateRows = analysis.Matches
            .Select(match => match.RowNumber)
            .ToHashSet();
        var skipped = CreateSkippedRows(analysis, duplicateRows).ToArray();
        if (!options.SkipDuplicateRows && duplicateRows.Count > 0)
        {
            return CreateApplyResult(
                false,
                analysis,
                null,
                [],
                skipped,
                "Duplicate CSV rows were found and SkipDuplicateRows is false; no rows were applied.");
        }

        var eligibleRows = analysis.Preview.Rows
            .Where(row => row.IsValid && !duplicateRows.Contains(row.RowNumber))
            .ToArray();
        if (eligibleRows.Length == 0)
        {
            return CreateApplyResult(
                false,
                analysis,
                null,
                [],
                skipped,
                "No valid non-duplicate CSV rows were available to apply.");
        }

        var batch = await CreateTransactionsInCopiedBookAsync(
            new GnuCashTransactionBatchCreateRequest(
                eligibleRows.Select(row => CreateTransactionRequest(row, options)).ToArray(),
                options.WorkingBookPath),
            cancellationToken).ConfigureAwait(false);
        var applied = batch.IsReady
            ? CreateAppliedRows(eligibleRows, batch).ToArray()
            : [];

        return CreateApplyResult(
            batch.IsReady,
            analysis,
            batch,
            applied,
            skipped,
            batch.Message);
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

    private GnuCashCsvTransactionImportApplyResult CreateApplyResult(
        bool isReady,
        GnuCashTransactionImportAnalysis analysis,
        GnuCashTransactionBatchCreateResult? nativeResult,
        IReadOnlyList<GnuCashCsvTransactionImportAppliedRow> applied,
        IReadOnlyList<GnuCashCsvTransactionImportSkippedRow> skipped,
        string message) =>
        new(
            isReady,
            BookPath,
            nativeResult?.WorkingBookPath,
            analysis,
            applied.Count,
            skipped.Count,
            applied,
            skipped,
            nativeResult,
            message);

    private static IEnumerable<GnuCashCsvTransactionImportSkippedRow> CreateSkippedRows(
        GnuCashTransactionImportAnalysis analysis,
        IReadOnlySet<int> duplicateRows)
    {
        foreach (var row in analysis.Preview.Rows.Where(row => !row.IsValid))
        {
            IReadOnlyList<GnuCashImportIssue> rowIssues = row.Issues.Count > 0
                ? row.Issues
                : analysis.Preview.Issues.Where(issue => issue.RowNumber == row.RowNumber).ToArray();
            yield return new GnuCashCsvTransactionImportSkippedRow(
                row.RowNumber,
                "InvalidRow",
                CreateInvalidRowMessage(rowIssues));
        }

        foreach (var rowNumber in duplicateRows.Order())
        {
            yield return new GnuCashCsvTransactionImportSkippedRow(
                rowNumber,
                "DuplicateRow",
                "An existing transaction already matches this CSV row.");
        }
    }

    private static string CreateInvalidRowMessage(IReadOnlyList<GnuCashImportIssue> issues) =>
        issues.Count == 0
            ? "The CSV row is invalid."
            : string.Join(" ", issues.Select(issue => issue.Message));

    private static GnuCashTransactionCreateRequest CreateTransactionRequest(
        GnuCashTransactionImportPreviewRow row,
        GnuCashCsvTransactionImportApplyOptions options) =>
        new(
            row.Description!,
            new DateTimeOffset(row.PostedDate!.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            [
                new GnuCashTransactionSplitCreateRequest(
                    options.ImportOptions.AccountId,
                    row.Value!,
                    Memo: row.Memo,
                    Action: "csv-import"),
                new GnuCashTransactionSplitCreateRequest(
                    options.TransferAccountId,
                    Negate(row.Value!),
                    Memo: row.Memo,
                    Action: "csv-import")
            ],
            options.CurrencySpace,
            options.CurrencyId,
            row.Number);

    private static IEnumerable<GnuCashCsvTransactionImportAppliedRow> CreateAppliedRows(
        IReadOnlyList<GnuCashTransactionImportPreviewRow> rows,
        GnuCashTransactionBatchCreateResult batch)
    {
        var createdByIndex = batch.CreatedTransactions.ToDictionary(transaction => transaction.Index);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            createdByIndex.TryGetValue(index, out var created);
            yield return new GnuCashCsvTransactionImportAppliedRow(
                row.RowNumber,
                row.Description!,
                row.Value!,
                created?.CreatedTransactionGuid);
        }
    }

}
