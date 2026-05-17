using System.Text.Json;
using GnuCash.DotNet.Bridge.Tests.Fixtures;
using GnuCash.DotNet.Protocol.Contracts;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class CliNativeWriteRoundTripTests
{
    [Fact]
    public async Task ValidateWriteRoundTripCommandReportsNativeApiFailureBeforeCopyingBook()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            [
                "validate-write-roundtrip",
                "--install-path",
                installFixture.InstallPath,
                "--source-book-path",
                bookFixture.BookPath,
                "--plain"
            ],
            writer,
            isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("GnuCash native write round-trip validation", output, StringComparison.Ordinal);
        Assert.Contains("Status:       Not ready", output, StringComparison.Ordinal);
        Assert.Contains(bookFixture.BookPath, output, StringComparison.Ordinal);
        Assert.Contains("libgnc-engine.dll", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeadlessCommandCanValidateNativeWriteRoundTripUsingProtocolRequest()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var payload = JsonSerializer.Serialize(new GnuCashNativeWriteRoundTripRequest(
            bookFixture.BookPath,
            InstallPath: installFixture.InstallPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.ValidateNativeWriteRoundTrip, payload);
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
        var status = JsonSerializer.Deserialize<GnuCashNativeWriteRoundTripStatus>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(status);
        Assert.False(status!.IsReady);
        Assert.Equal(Path.GetFullPath(bookFixture.BookPath), status.SourceBookPath);
        Assert.Contains("libgnc-engine.dll", status.Message, StringComparison.Ordinal);
    }
}
