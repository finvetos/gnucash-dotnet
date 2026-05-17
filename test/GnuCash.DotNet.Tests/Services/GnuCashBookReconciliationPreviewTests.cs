using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookReconciliationPreviewTests
{
    [Fact]
    public async Task PreviewReconciliationAsyncComparesStatementBalance()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient();

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var balanced = await book.PreviewReconciliationAsync(
            "11111111111111111111111111111111",
            new GnuCashAmount("0/100", 0, 100),
            TestContext.Current.CancellationToken);
        var unbalanced = await book.PreviewReconciliationAsync(
            "11111111111111111111111111111111",
            new GnuCashAmount("100000/100", 100000, 100),
            TestContext.Current.CancellationToken);

        Assert.True(balanced.IsBalanced);
        Assert.Equal(0m, balanced.ActualEndingBalance.DecimalValue);
        Assert.Equal(0m, balanced.Variance.DecimalValue);

        Assert.False(unbalanced.IsBalanced);
        Assert.Equal(-1000m, unbalanced.Variance.DecimalValue);
        Assert.Equal(1, unbalanced.Summary.UnreconciledSplitCount);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location
            }));
}
