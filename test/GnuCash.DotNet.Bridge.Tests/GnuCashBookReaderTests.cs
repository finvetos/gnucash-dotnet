using GnuCash.DotNet.Bridge.Books;
using GnuCash.DotNet.Bridge.Tests.Fixtures;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class GnuCashBookReaderTests
{
    [Fact]
    public void OpenReturnsBookSummaryForGnuCashXml()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var reader = new GnuCashBookReader();

        var summary = reader.Open(fixture.BookPath);

        Assert.Equal(Path.GetFullPath(fixture.BookPath), summary.BookPath);
        Assert.Equal("GnuCashXml", summary.FileFormat);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", summary.BookId);
        Assert.Equal(2, summary.CommodityCount);
        Assert.Equal(3, summary.AccountCount);
        Assert.Equal(1, summary.TransactionCount);
        Assert.Equal(2, summary.SplitCount);
        Assert.Equal(1, summary.PriceCount);
    }

    [Fact]
    public void ListAccountsReturnsAccountTreeShape()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var reader = new GnuCashBookReader();

        var accounts = reader.ListAccounts(fixture.BookPath);

        Assert.Equal(3, accounts.Count);
        Assert.Contains(accounts, account => account.Name == "Root Account" && account.IsPlaceholder);
        Assert.Contains(accounts, account =>
            account.Name == "Checking" &&
            account.Type == "BANK" &&
            account.ParentId == "00000000000000000000000000000000" &&
            account.CommoditySpace == "CURRENCY" &&
            account.CommodityId == "USD");
    }

    [Fact]
    public void ListCommoditiesReturnsCurrenciesAndSecurities()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var reader = new GnuCashBookReader();

        var commodities = reader.ListCommodities(fixture.BookPath);

        Assert.Equal(2, commodities.Count);
        Assert.Contains(commodities, commodity =>
            commodity.Space == "CURRENCY" &&
            commodity.Id == "USD" &&
            commodity.Fraction == 100);
        Assert.Contains(commodities, commodity =>
            commodity.Space == "NASDAQ" &&
            commodity.Id == "MSFT" &&
            commodity.Fraction == 10000);
    }

    [Fact]
    public void ListTransactionsReturnsSplitsAndRationalAmounts()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var reader = new GnuCashBookReader();

        var transactions = reader.ListTransactions(fixture.BookPath);

        var transaction = Assert.Single(transactions);
        Assert.Equal("DEP-1", transaction.Number);
        Assert.Equal("Opening deposit", transaction.Description);
        Assert.Equal("CURRENCY", transaction.CurrencySpace);
        Assert.Equal("USD", transaction.CurrencyId);
        Assert.Equal(2, transaction.Splits.Count);
        Assert.Contains(transaction.Splits, split =>
            split.AccountId == "11111111111111111111111111111111" &&
            split.Value.Numerator == 100000 &&
            split.Value.Denominator == 100);
        Assert.Contains(transaction.Splits, split =>
            split.AccountId == "22222222222222222222222222222222" &&
            split.ReconciledState == "c" &&
            split.Memo == "Initial funding");
    }

    [Fact]
    public void ListPricesReturnsPriceDatabaseEntries()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var reader = new GnuCashBookReader();

        var price = Assert.Single(reader.ListPrices(fixture.BookPath));

        Assert.Equal("66666666666666666666666666666666", price.Id);
        Assert.Equal("NASDAQ", price.CommoditySpace);
        Assert.Equal("MSFT", price.CommodityId);
        Assert.Equal("CURRENCY", price.CurrencySpace);
        Assert.Equal("USD", price.CurrencyId);
        Assert.Equal("last", price.Type);
        Assert.Equal("25000/100", price.Value.RawValue);
    }

    [Fact]
    public void ReaderSupportsCompressedGnuCashXml()
    {
        using var fixture = GnuCashBookFixture.CreateCompressedXml();
        var reader = new GnuCashBookReader();

        var summary = reader.Open(fixture.BookPath);

        Assert.Equal(3, summary.AccountCount);
        Assert.Equal(1, summary.TransactionCount);
    }
}
