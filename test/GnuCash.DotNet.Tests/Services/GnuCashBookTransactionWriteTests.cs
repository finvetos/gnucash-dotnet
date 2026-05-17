using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookTransactionWriteTests
{
    [Fact]
    public async Task CreateTransactionInCopiedBookAsyncMapsNativeWriteStatus()
    {
        using var fixture = GnuCashBookFixture.Create();
        var missingInstallPath = Path.Combine(fixture.DirectoryPath, "missing-install");
        var workingBookPath = Path.Combine(fixture.DirectoryPath, "transaction-copy.gnucash");
        Directory.CreateDirectory(missingInstallPath);
        var client = CreateClient(missingInstallPath);

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var result = await book.CreateTransactionInCopiedBookAsync(
            new GnuCashTransactionCreateRequest(
                "Coffee",
                new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
                [
                    new GnuCashTransactionSplitCreateRequest(
                        "11111111111111111111111111111111",
                        new GnuCashAmount("-450/100", -450, 100)),
                    new GnuCashTransactionSplitCreateRequest(
                        "22222222222222222222222222222222",
                        new GnuCashAmount("450/100", 450, 100))
                ],
                WorkingBookPath: workingBookPath),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsReady);
        Assert.Equal(Path.GetFullPath(fixture.BookPath), result.SourceBookPath);
        Assert.Equal(Path.GetFullPath(workingBookPath), result.WorkingBookPath);
        Assert.Equal("Coffee", result.Description);
        Assert.Equal(2, result.SplitCount);
        Assert.False(result.FoundAfterReopen);
        Assert.Contains("Install GnuCash for Windows first", result.Message, StringComparison.Ordinal);
    }

    private static GnuCashClient CreateClient(string installPath) =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
                InstallPath = installPath
            }));
}
