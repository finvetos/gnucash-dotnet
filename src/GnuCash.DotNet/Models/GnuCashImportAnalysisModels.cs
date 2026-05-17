namespace GnuCash.DotNet.Models;

/// <summary>
/// CSV import preview enriched with duplicate and match information from the opened book.
/// </summary>
public sealed record GnuCashTransactionImportAnalysis(
    GnuCashTransactionImportPreview Preview,
    IReadOnlyList<GnuCashTransactionImportMatch> Matches,
    int DuplicateRows,
    int ReadyRows,
    int BlockedRows);

/// <summary>
/// Existing transaction candidate that appears to match an import preview row.
/// </summary>
public sealed record GnuCashTransactionImportMatch(
    int RowNumber,
    string TransactionId,
    string SplitId,
    string AccountId,
    DateTimeOffset? PostedAt,
    string? Description,
    GnuCashAmount Value,
    string MatchCode,
    string Message);
