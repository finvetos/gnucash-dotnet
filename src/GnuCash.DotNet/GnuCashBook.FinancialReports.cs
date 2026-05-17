using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    private static readonly string[] AssetAccountTypes =
    [
        "ASSET",
        "BANK",
        "CASH",
        "STOCK",
        "MUTUAL",
        "RECEIVABLE"
    ];

    private static readonly string[] LiabilityAccountTypes =
    [
        "LIABILITY",
        "CREDIT",
        "PAYABLE"
    ];

    /// <summary>
    /// Creates a trial balance report from account balances.
    /// </summary>
    public async Task<GnuCashTrialBalanceReport> CreateTrialBalanceReportAsync(
        CancellationToken cancellationToken = default)
    {
        var balances = await ListAccountBalancesAsync(cancellationToken).ConfigureAwait(false);
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var accountsById = accounts.ToDictionary(account => account.Id, StringComparer.OrdinalIgnoreCase);
        var rows = balances
            .Select(balance => CreateTrialBalanceRow(balance, accountsById[balance.AccountId]))
            .ToArray();
        var debitTotal = Sum(rows.Select(row => row.Debit));
        var creditTotal = Sum(rows.Select(row => row.Credit));

        return new GnuCashTrialBalanceReport(
            BookPath,
            DateTimeOffset.UtcNow,
            rows,
            debitTotal,
            creditTotal,
            AreEquivalentAmounts(debitTotal, creditTotal));
    }

    /// <summary>
    /// Creates a balance sheet report from account balances.
    /// </summary>
    public async Task<GnuCashBalanceSheetReport> CreateBalanceSheetReportAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await CreateAccountSummaryReportAsync(cancellationToken).ConfigureAwait(false);
        var assets = CreateBalanceSheetSection("Assets", summary.Rows, AssetAccountTypes, normalizeCredits: false);
        var liabilities = CreateBalanceSheetSection("Liabilities", summary.Rows, LiabilityAccountTypes, normalizeCredits: true);
        var equity = CreateBalanceSheetSection("Equity", summary.Rows, ["EQUITY"], normalizeCredits: true);
        var liabilitiesAndEquity = Sum([liabilities.Total, equity.Total]);

        return new GnuCashBalanceSheetReport(
            BookPath,
            DateTimeOffset.UtcNow,
            assets,
            liabilities,
            equity,
            liabilitiesAndEquity,
            AreEquivalentAmounts(assets.Total, liabilitiesAndEquity));
    }

    /// <summary>
    /// Creates an income statement report from income and expense account balances.
    /// </summary>
    public async Task<GnuCashIncomeStatementReport> CreateIncomeStatementReportAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await CreateAccountSummaryReportAsync(cancellationToken).ConfigureAwait(false);
        var income = CreateIncomeStatementSection("Income", summary.Rows, ["INCOME"], normalizeCredits: true);
        var expenses = CreateIncomeStatementSection("Expenses", summary.Rows, ["EXPENSE"], normalizeCredits: false);

        return new GnuCashIncomeStatementReport(
            BookPath,
            DateTimeOffset.UtcNow,
            income,
            expenses,
            Subtract(income.Total, expenses.Total));
    }

    /// <summary>
    /// Creates a cash-flow report from cash-like account splits.
    /// </summary>
    public async Task<GnuCashCashFlowReport> CreateCashFlowReportAsync(
        GnuCashTransactionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await ListAccountsAsync(cancellationToken).ConfigureAwait(false);
        var cashAccounts = accounts
            .Where(account => IsAccountType(account.Type, "BANK", "CASH"))
            .ToDictionary(account => account.Id, StringComparer.OrdinalIgnoreCase);
        var transactions = await ListTransactionsAsync(query, cancellationToken).ConfigureAwait(false);
        var rows = transactions
            .SelectMany(transaction => transaction.Splits
                .Where(split => cashAccounts.ContainsKey(split.AccountId))
                .Where(split => Matches(split, query))
                .Select(split => CreateCashFlowRow(transaction, split, cashAccounts[split.AccountId])))
            .ToArray();
        var inflow = Sum(rows.Where(row => row.Amount.Numerator is >= 0).Select(row => row.Amount));
        var outflow = Sum(rows.Where(row => row.Amount.Numerator is < 0).Select(row => Absolute(row.Amount)));
        var netChange = Sum(rows.Select(row => row.Amount));

        return new GnuCashCashFlowReport(
            BookPath,
            DateTimeOffset.UtcNow,
            query,
            rows,
            inflow,
            outflow,
            netChange);
    }

    /// <summary>
    /// Creates a cash-flow report from cash-like account splits.
    /// </summary>
    public Task<GnuCashCashFlowReport> CreateCashFlowReportAsync(
        CancellationToken cancellationToken) =>
        CreateCashFlowReportAsync(null, cancellationToken);

    private static GnuCashTrialBalanceRow CreateTrialBalanceRow(
        GnuCashAccountBalance balance,
        GnuCashAccount account)
    {
        var isCredit = balance.Balance.Numerator is < 0;
        return new GnuCashTrialBalanceRow(
            account.Id,
            account.Name,
            account.Type,
            account.CommoditySpace,
            account.CommodityId,
            isCredit ? ZeroAmount() : balance.Balance,
            isCredit ? Absolute(balance.Balance) : ZeroAmount());
    }

    private static GnuCashBalanceSheetSection CreateBalanceSheetSection(
        string name,
        IReadOnlyList<GnuCashAccountSummaryRow> rows,
        IReadOnlyList<string> accountTypes,
        bool normalizeCredits)
    {
        var sectionRows = rows
            .Where(row => IsAccountType(row.AccountType, accountTypes))
            .ToArray();
        var total = Sum(sectionRows.Select(row => normalizeCredits ? Absolute(row.Balance) : row.Balance));

        return new GnuCashBalanceSheetSection(name, sectionRows, total);
    }

    private static GnuCashIncomeStatementSection CreateIncomeStatementSection(
        string name,
        IReadOnlyList<GnuCashAccountSummaryRow> rows,
        IReadOnlyList<string> accountTypes,
        bool normalizeCredits)
    {
        var sectionRows = rows
            .Where(row => IsAccountType(row.AccountType, accountTypes))
            .ToArray();
        var total = Sum(sectionRows.Select(row => normalizeCredits ? Absolute(row.Balance) : row.Balance));

        return new GnuCashIncomeStatementSection(name, sectionRows, total);
    }

    private static GnuCashCashFlowRow CreateCashFlowRow(
        GnuCashTransaction transaction,
        GnuCashSplit split,
        GnuCashAccount account) =>
        new(
            account.Id,
            account.Name,
            transaction.Id,
            transaction.PostedAt,
            transaction.Description,
            split.Value);

    private static bool IsAccountType(string accountType, params string[] expectedTypes) =>
        IsAccountType(accountType, (IReadOnlyList<string>)expectedTypes);

    private static bool IsAccountType(string accountType, IReadOnlyList<string> expectedTypes) =>
        expectedTypes.Any(expectedType => MatchesExact(accountType, expectedType));
}
