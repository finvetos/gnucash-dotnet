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

    [Fact]
    public async Task ApplyCsvTransactionImportToCopiedBookAsyncSkipsDuplicatesAndBuildsNativeBatch()
    {
        using var fixture = GnuCashBookFixture.Create();
        var missingInstallPath = Path.Combine(fixture.DirectoryPath, "missing-install");
        var workingBookPath = Path.Combine(fixture.DirectoryPath, "csv-apply-copy.gnucash");
        var csvPath = Path.Combine(fixture.DirectoryPath, "statement.csv");
        Directory.CreateDirectory(missingInstallPath);
        await File.WriteAllTextAsync(
            csvPath,
            """
            Date,Description,Amount,Number,Memo
            2026-01-01,Opening deposit,1000.00,1000,Duplicate
            2026-01-05,Coffee,-4.50,1001,Debit card
            """,
            TestContext.Current.CancellationToken);
        var client = CreateClient(missingInstallPath);

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var result = await book.ApplyCsvTransactionImportToCopiedBookAsync(
            csvPath,
            new GnuCashCsvTransactionImportApplyOptions(
                new GnuCashCsvTransactionImportOptions(
                    "11111111111111111111111111111111",
                    NumberColumn: "Number",
                    MemoColumn: "Memo"),
                "22222222222222222222222222222222",
                WorkingBookPath: workingBookPath),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsReady);
        Assert.Equal(Path.GetFullPath(fixture.BookPath), result.SourceBookPath);
        Assert.Empty(result.Applied);

        var skipped = Assert.Single(result.Skipped);
        Assert.Equal(2, skipped.RowNumber);
        Assert.Equal("DuplicateRow", skipped.Code);

        Assert.NotNull(result.NativeResult);
        Assert.Equal(Path.GetFullPath(workingBookPath), result.NativeResult!.WorkingBookPath);
        Assert.Equal(1, result.NativeResult.RequestedTransactionCount);
        Assert.Equal(2, result.NativeResult.SplitCount);
        Assert.Contains("Install GnuCash for Windows first", result.Message, StringComparison.Ordinal);
    }

    private static GnuCashClient CreateClient() =>
        CreateClient(null);

    private static GnuCashClient CreateClient(string? installPath) =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
                InstallPath = installPath
            }));
}
