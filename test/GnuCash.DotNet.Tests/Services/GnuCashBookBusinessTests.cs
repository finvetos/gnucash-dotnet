using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookBusinessTests
{
    [Fact]
    public async Task CreateCustomerInCopiedBookAsyncMapsNativeWriteStatus()
    {
        using var fixture = GnuCashBookFixture.Create();
        var missingInstallPath = Path.Combine(fixture.DirectoryPath, "missing-install");
        var workingBookPath = Path.Combine(fixture.DirectoryPath, "customer-copy.gnucash");
        Directory.CreateDirectory(missingInstallPath);
        var client = CreateClient(missingInstallPath);

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var result = await book.CreateCustomerInCopiedBookAsync(
            new GnuCashCustomerCreateRequest(
                "CUST-1",
                "Test Customer",
                WorkingBookPath: workingBookPath),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsReady);
        Assert.Equal(Path.GetFullPath(fixture.BookPath), result.SourceBookPath);
        Assert.Equal(Path.GetFullPath(workingBookPath), result.WorkingBookPath);
        Assert.Equal("CUST-1", result.CustomerId);
        Assert.Equal("Test Customer", result.CustomerName);
        Assert.Equal("CURRENCY", result.CurrencySpace);
        Assert.Equal("USD", result.CurrencyId);
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
