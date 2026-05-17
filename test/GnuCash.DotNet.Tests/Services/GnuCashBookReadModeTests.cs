using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookReadModeTests
{
    [Fact]
    public async Task NativeThenXmlReadModeFallsBackToXmlWhenNativeRuntimeIsUnavailable()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(
            GnuCashBookReadMode.NativeThenXml,
            Path.Combine(fixture.DirectoryPath, "missing-install"));

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var accounts = await book.ListAccountsAsync(TestContext.Current.CancellationToken);

        Assert.Equal("GnuCashXml", book.Info.FileFormat);
        Assert.Contains(accounts, account => account.Name == "Checking");
    }

    [Fact]
    public async Task NativeReadModeFailsWhenNativeRuntimeIsUnavailable()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(
            GnuCashBookReadMode.Native,
            Path.Combine(fixture.DirectoryPath, "missing-install"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken));

        Assert.Contains("Install GnuCash for Windows first", error.Message, StringComparison.Ordinal);
    }

    private static GnuCashClient CreateClient(GnuCashBookReadMode readMode, string installPath) =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
                InstallPath = installPath,
                ReadMode = readMode
            }));
}
