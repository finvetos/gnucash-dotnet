using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookImportAnalysisTests
{
    [Fact]
    public async Task AnalyzeCsvTransactionImportAsyncFindsDuplicateRows()
    {
        using var fixture = GnuCashBookFixture.Create();
        var csvPath = Path.Combine(fixture.DirectoryPath, "statement.csv");
        await File.WriteAllTextAsync(
            csvPath,
            """
            Date,Description,Amount
            2026-01-01,Opening deposit,1000.00
            2026-01-05,Coffee,-4.50
            """,
            TestContext.Current.CancellationToken);
        var client = CreateClient();

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var analysis = await book.AnalyzeCsvTransactionImportAsync(
            csvPath,
            new GnuCashCsvTransactionImportOptions("11111111111111111111111111111111"),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, analysis.Preview.ValidRows);
        Assert.Equal(1, analysis.DuplicateRows);
        Assert.Equal(1, analysis.ReadyRows);
        Assert.Equal(1, analysis.BlockedRows);

        var match = Assert.Single(analysis.Matches);
        Assert.Equal(2, match.RowNumber);
        Assert.Equal("33333333333333333333333333333333", match.TransactionId);
        Assert.Equal("44444444444444444444444444444444", match.SplitId);
        Assert.Equal("SameAccountDateAmountDescription", match.MatchCode);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location
            }));
}
