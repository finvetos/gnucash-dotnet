using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookLifecycleAndWriteTests
{
    [Fact]
    public async Task CreateBookAsyncCreatesOpenableXmlBook()
    {
        var directory = Path.Combine(Path.GetTempPath(), "gnucash-dotnet-create-book", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "created.gnucash");
        var client = CreateClient();

        try
        {
            var book = await client.CreateBookAsync(path, cancellationToken: TestContext.Current.CancellationToken);
            var accounts = await book.ListAccountsAsync(TestContext.Current.CancellationToken);
            var currencies = await book.ListCurrenciesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(Path.GetFullPath(path), book.BookPath);
            Assert.Single(accounts);
            Assert.Equal("ROOT", accounts[0].Type);
            Assert.Single(currencies);
            Assert.Equal("USD", currencies[0].Id);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CopiedXmlWriteApisCreateAccountsPricesAndReconciliationUpdates()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient();
        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var accountCopy = Path.Combine(fixture.DirectoryPath, "account-copy.gnucash");
        var priceCopy = Path.Combine(fixture.DirectoryPath, "price-copy.gnucash");
        var reconcileCopy = Path.Combine(fixture.DirectoryPath, "reconcile-copy.gnucash");

        var account = await book.CreateAccountInCopiedBookAsync(
            new GnuCashAccountCreateRequest(
                "Savings",
                "BANK",
                "00000000000000000000000000000000",
                WorkingBookPath: accountCopy),
            TestContext.Current.CancellationToken);
        var price = await book.CreatePriceInCopiedBookAsync(
            new GnuCashPriceCreateRequest(
                "NASDAQ",
                "MSFT",
                "CURRENCY",
                "USD",
                new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero),
                new GnuCashAmount("25100/100", 25100, 100),
                WorkingBookPath: priceCopy),
            TestContext.Current.CancellationToken);
        var reconciliation = await book.MarkSplitsReconciledInCopiedBookAsync(
            new GnuCashReconciliationWriteRequest(
                ["44444444444444444444444444444444"],
                "y",
                new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero),
                reconcileCopy),
            TestContext.Current.CancellationToken);

        Assert.True(account.IsReady);
        Assert.Equal(4, account.AfterAccountCount);
        Assert.True(price.IsReady);
        Assert.Equal(2, price.AfterPriceCount);
        Assert.True(reconciliation.IsReady);
        Assert.Equal(1, reconciliation.UpdatedSplitCount);
        var reconciledBook = await client.OpenBookAsync(reconcileCopy, TestContext.Current.CancellationToken);
        var split = await reconciledBook.GetSplitByIdAsync(
            "44444444444444444444444444444444",
            TestContext.Current.CancellationToken);
        Assert.Equal("y", split!.ReconciledState);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location
            }));
}
