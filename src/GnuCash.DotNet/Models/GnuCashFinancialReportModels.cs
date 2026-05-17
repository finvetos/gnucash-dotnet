namespace GnuCash.DotNet.Models;

/// <summary>
/// Trial balance report for an opened book.
/// </summary>
public sealed record GnuCashTrialBalanceReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<GnuCashTrialBalanceRow> Rows,
    GnuCashAmount DebitTotal,
    GnuCashAmount CreditTotal,
    bool IsBalanced);

/// <summary>
/// Row in a trial balance report.
/// </summary>
public sealed record GnuCashTrialBalanceRow(
    string AccountId,
    string AccountName,
    string AccountType,
    string? CommoditySpace,
    string? CommodityId,
    GnuCashAmount Debit,
    GnuCashAmount Credit);

/// <summary>
/// Balance sheet report for an opened book.
/// </summary>
public sealed record GnuCashBalanceSheetReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    GnuCashBalanceSheetSection Assets,
    GnuCashBalanceSheetSection Liabilities,
    GnuCashBalanceSheetSection Equity,
    GnuCashAmount LiabilitiesAndEquity,
    bool IsBalanced);

/// <summary>
/// Balance sheet section grouped by account type.
/// </summary>
public sealed record GnuCashBalanceSheetSection(
    string Name,
    IReadOnlyList<GnuCashAccountSummaryRow> Rows,
    GnuCashAmount Total);

/// <summary>
/// Income statement report for an opened book.
/// </summary>
public sealed record GnuCashIncomeStatementReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    GnuCashIncomeStatementSection Income,
    GnuCashIncomeStatementSection Expenses,
    GnuCashAmount NetIncome);

/// <summary>
/// Income statement section grouped by account type.
/// </summary>
public sealed record GnuCashIncomeStatementSection(
    string Name,
    IReadOnlyList<GnuCashAccountSummaryRow> Rows,
    GnuCashAmount Total);

/// <summary>
/// Cash-flow report for cash-like accounts in an opened book.
/// </summary>
public sealed record GnuCashCashFlowReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    GnuCashTransactionQuery? Query,
    IReadOnlyList<GnuCashCashFlowRow> Rows,
    GnuCashAmount Inflow,
    GnuCashAmount Outflow,
    GnuCashAmount NetChange);

/// <summary>
/// Cash-flow row for a single split.
/// </summary>
public sealed record GnuCashCashFlowRow(
    string AccountId,
    string AccountName,
    string TransactionId,
    DateTimeOffset? PostedAt,
    string? Description,
    GnuCashAmount Amount);

/// <summary>
/// Balances grouped by commodity or currency.
/// </summary>
public sealed record GnuCashMultiCurrencyBalanceReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<GnuCashCommodityBalance> Balances);

/// <summary>
/// Balance total for one commodity or currency.
/// </summary>
public sealed record GnuCashCommodityBalance(
    string? CommoditySpace,
    string? CommodityId,
    GnuCashAmount Balance,
    int AccountCount);

/// <summary>
/// Portfolio valuation report using latest known prices.
/// </summary>
public sealed record GnuCashPortfolioValuationReport(
    string BookPath,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<GnuCashPortfolioValuationRow> Rows);

/// <summary>
/// Portfolio valuation row for one security commodity.
/// </summary>
public sealed record GnuCashPortfolioValuationRow(
    string CommoditySpace,
    string CommodityId,
    string? CommodityName,
    string? CurrencySpace,
    string? CurrencyId,
    GnuCashAmount Quantity,
    GnuCashPrice? LatestPrice,
    decimal? MarketValue,
    int AccountCount);
