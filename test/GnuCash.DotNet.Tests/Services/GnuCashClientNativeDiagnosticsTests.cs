using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashClientNativeDiagnosticsTests
{
    [Fact]
    public async Task NativeDiagnosticApisUseBridgeProtocol()
    {
        using var install = GnuCashInstallFixture.Create();
        using var book = GnuCashBookFixture.Create();
        var client = CreateClient(install.InstallPath);

        var api = await client.ValidateNativeApiAsync(TestContext.Current.CancellationToken);
        var inventory = await client.InventoryNativeExportsAsync(TestContext.Current.CancellationToken);
        var session = await client.ValidateNativeSessionAsync(
            book.BookPath,
            TestContext.Current.CancellationToken);
        var parity = await client.ValidateNativeReadParityAsync(
            book.BookPath,
            TestContext.Current.CancellationToken);

        Assert.False(api.IsReady);
        Assert.Equal(install.InstallPath, api.InstallPath);
        Assert.Contains(api.CheckedPaths, path => path.EndsWith("libgnc-engine.dll", StringComparison.OrdinalIgnoreCase));
        Assert.False(inventory.IsReady);
        Assert.Equal(3, inventory.LibraryCount);
        Assert.Equal(Path.GetFullPath(book.BookPath), session.BookPath);
        Assert.False(session.IsReady);
        Assert.Equal(Path.GetFullPath(book.BookPath), parity.BookPath);
        Assert.False(parity.IsReady);
    }

    [Fact]
    public async Task ValidateNativeWriteRoundTripAsyncReportsMissingRuntimeWithoutMutatingSourceBook()
    {
        using var install = GnuCashInstallFixture.Create();
        using var book = GnuCashBookFixture.Create();
        var workingPath = Path.Combine(book.DirectoryPath, "working.gnucash");
        var before = await File.ReadAllTextAsync(book.BookPath, TestContext.Current.CancellationToken);
        var client = CreateClient(install.InstallPath);

        var status = await client.ValidateNativeWriteRoundTripAsync(
            book.BookPath,
            workingPath,
            TestContext.Current.CancellationToken);
        var after = await File.ReadAllTextAsync(book.BookPath, TestContext.Current.CancellationToken);

        Assert.False(status.IsReady);
        Assert.Equal(Path.GetFullPath(book.BookPath), status.SourceBookPath);
        Assert.Equal(Path.GetFullPath(workingPath), status.WorkingBookPath);
        Assert.Equal(before, after);
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
