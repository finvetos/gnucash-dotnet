using System.Text.Json;
using GnuCash.DotNet.Bridge.Tests.Fixtures;
using GnuCash.DotNet.Protocol.Contracts;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class CliNativeExportInventoryTests
{
    [Fact]
    public async Task InventoryExportsCommandReportsLibraryInspectionErrors()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            ["inventory-exports", "--install-path", fixture.InstallPath, "--plain"],
            writer,
            isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("GnuCash native export inventory", output, StringComparison.Ordinal);
        Assert.Contains("Status:    Not ready", output, StringComparison.Ordinal);
        Assert.Contains("libgnc-engine.dll", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InventoryExportsCommandCanReturnJson()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            ["inventory-exports", "--install-path", fixture.InstallPath, "--json"],
            writer,
            isOutputRedirected: false);

        var status = JsonSerializer.Deserialize<GnuCashNativeExportInventoryStatus>(
            writer.ToString(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(1, exitCode);
        Assert.NotNull(status);
        Assert.False(status!.IsReady);
        Assert.Equal(3, status.LibraryCount);
        Assert.Contains(status.Libraries, library => library.Name == "libgnc-engine.dll");
    }

    [Fact]
    public async Task HeadlessCommandCanInventoryNativeExportsUsingProtocolRequest()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var locatePayload = JsonSerializer.Serialize(new LocateGnuCashRequest(fixture.InstallPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.InventoryNativeExports, locatePayload);
        var input = new StringReader(JsonSerializer.Serialize(request) + Environment.NewLine);
        var output = new StringWriter();
        var error = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            ["headless", "--stdio"],
            output,
            error,
            isOutputRedirected: true,
            input: input);

        var response = JsonSerializer.Deserialize<BridgeResponse>(
            output.ToString().Trim(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var status = JsonSerializer.Deserialize<GnuCashNativeExportInventoryStatus>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(status);
        Assert.False(status!.IsReady);
        Assert.Equal(3, status.LibraryCount);
    }
}
