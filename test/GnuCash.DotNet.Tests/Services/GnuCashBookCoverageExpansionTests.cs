using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookCoverageExpansionTests
{
    [Fact]
    public async Task OpenBookAsyncCanNavigateAccountsCommoditiesPricesTransactionsAndSplits()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient();

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var currencies = await book.ListCurrenciesAsync(TestContext.Current.CancellationToken);
        var usd = await book.GetCommodityAsync("CURRENCY", "USD", TestContext.Current.CancellationToken);
        var checkingPath = await book.GetAccountPathAsync(
            "11111111111111111111111111111111",
            TestContext.Current.CancellationToken);
        var transaction = await book.GetTransactionByIdAsync(
            "33333333333333333333333333333333",
            TestContext.Current.CancellationToken);
        var split = await book.GetSplitByIdAsync(
            "44444444444444444444444444444444",
            TestContext.Current.CancellationToken);
        var latestPrice = await book.GetLatestPriceAsync(
            "NASDAQ",
            "MSFT",
            "CURRENCY",
            "USD",
            TestContext.Current.CancellationToken);

        Assert.Single(currencies);
        Assert.NotNull(usd);
        Assert.Equal("US Dollar", usd!.Name);
        Assert.NotNull(checkingPath);
        Assert.Equal("Root Account:Checking", checkingPath!.FullName);
        Assert.NotNull(transaction);
        Assert.Equal("Opening deposit", transaction!.Description);
        Assert.NotNull(split);
        Assert.Equal("100000/100", split!.Value.RawValue);
        Assert.NotNull(latestPrice);
        Assert.Equal(250m, latestPrice!.Value.DecimalValue);
    }

    [Fact]
    public async Task OpenBookAsyncCanCreateFinancialAndPortfolioReports()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient();

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var trialBalance = await book.CreateTrialBalanceReportAsync(TestContext.Current.CancellationToken);
        var balanceSheet = await book.CreateBalanceSheetReportAsync(TestContext.Current.CancellationToken);
        var incomeStatement = await book.CreateIncomeStatementReportAsync(TestContext.Current.CancellationToken);
        var cashFlow = await book.CreateCashFlowReportAsync(TestContext.Current.CancellationToken);
        var multiCurrency = await book.CreateMultiCurrencyBalanceReportAsync(TestContext.Current.CancellationToken);
        var portfolio = await book.CreatePortfolioValuationReportAsync(TestContext.Current.CancellationToken);

        Assert.True(trialBalance.IsBalanced);
        Assert.Equal("100000/100", trialBalance.DebitTotal.RawValue);
        Assert.True(balanceSheet.IsBalanced);
        Assert.Equal("100000/100", balanceSheet.Assets.Total.RawValue);
        Assert.Equal("0/1", incomeStatement.NetIncome.RawValue);
        Assert.Equal("100000/100", cashFlow.NetChange.RawValue);
        Assert.Contains(multiCurrency.Balances, balance =>
            balance.CommoditySpace == "CURRENCY" &&
            balance.CommodityId == "USD");
        var security = Assert.Single(portfolio.Rows);
        Assert.Equal("NASDAQ", security.CommoditySpace);
        Assert.Equal("MSFT", security.CommodityId);
        Assert.NotNull(security.LatestPrice);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location
            }));
}
