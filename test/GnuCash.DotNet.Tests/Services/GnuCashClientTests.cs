using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Protocol.Contracts;
using GnuCash.DotNet.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashClientTests
{
    [Fact]
    public void CreatesPingRequestForCurrentProtocol()
    {
        var client = new GnuCashClient(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions()));

        var request = client.CreatePingRequest();

        Assert.Equal(BridgeRequestKind.Ping, request.Kind);
        Assert.Equal(BridgeProtocol.CurrentVersion, request.ProtocolVersion);
    }

    [Fact]
    public async Task ValidateInstallationAsyncUsesBridgeHeadlessProtocol()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
            InstallPath = fixture.InstallPath
        });

        var status = await client.ValidateInstallationAsync(TestContext.Current.CancellationToken);

        Assert.True(status.IsReady);
        Assert.Equal(fixture.InstallPath, status.InstallPath);
        Assert.Contains(fixture.InstallPath, status.CheckedPaths);
    }

    [Fact]
    public async Task ValidateInstallationAsyncReturnsNotReadyStatusWhenGnuCashIsMissing()
    {
        var installPath = Path.Combine(Path.GetTempPath(), "gnucash-dotnet-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installPath);
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
            InstallPath = installPath
        });

        try
        {
            var status = await client.ValidateInstallationAsync(TestContext.Current.CancellationToken);

            Assert.False(status.IsReady);
            Assert.Contains("Install GnuCash for Windows first", status.Message, StringComparison.Ordinal);
            Assert.NotEmpty(status.MissingPaths);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateInstallationAsyncFailsClearlyWhenBridgeCannotBeFound()
    {
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.exe")
        });

        var error = await Assert.ThrowsAsync<FileNotFoundException>(
            () => client.ValidateInstallationAsync(TestContext.Current.CancellationToken));

        Assert.Contains("BridgeExecutablePath does not exist", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static GnuCashClient CreateClient(GnuCashBridgeOptions options) =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(options));

    private sealed class GnuCashInstallFixture : IDisposable
    {
        private GnuCashInstallFixture(string installPath)
        {
            InstallPath = installPath;
        }

        public string InstallPath { get; }

        public static GnuCashInstallFixture Create()
        {
            var installPath = Path.Combine(
                Path.GetTempPath(),
                "gnucash-dotnet-tests",
                Guid.NewGuid().ToString("N"));

            CreateFile(installPath, "bin", "gnucash.exe");
            CreateFile(installPath, "bin", "gnucash-cli.exe");
            CreateFile(installPath, "bin", "libgnc-core-utils.dll");
            CreateFile(installPath, "bin", "libgnc-engine.dll");
            Directory.CreateDirectory(Path.Combine(installPath, "etc", "gnucash"));
            Directory.CreateDirectory(Path.Combine(installPath, "lib", "gnucash"));
            Directory.CreateDirectory(Path.Combine(installPath, "share", "gnucash"));

            return new GnuCashInstallFixture(installPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(InstallPath))
            {
                Directory.Delete(InstallPath, recursive: true);
            }
        }

        private static void CreateFile(string root, params string[] parts)
        {
            var path = Path.Combine([root, .. parts]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
        }
    }
}
