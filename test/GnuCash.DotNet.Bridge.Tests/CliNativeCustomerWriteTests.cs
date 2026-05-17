using System.Text.Json;
using GnuCash.DotNet.Bridge.Tests.Fixtures;
using GnuCash.DotNet.Protocol.Contracts;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class CliNativeCustomerWriteTests
{
    [Fact]
    public async Task ValidateCustomerWriteCommandReportsNativeApiFailureBeforeCopyingBook()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            [
                "validate-customer-write",
                "--install-path",
                installFixture.InstallPath,
                "--source-book-path",
                bookFixture.BookPath,
                "--customer-id",
                "CUST-1",
                "--customer-name",
                "Test Customer",
                "--plain"
            ],
            writer,
            isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("GnuCash native customer write validation", output, StringComparison.Ordinal);
        Assert.Contains("Status:       Not ready", output, StringComparison.Ordinal);
        Assert.Contains(bookFixture.BookPath, output, StringComparison.Ordinal);
        Assert.Contains("libgnc-engine.dll", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeadlessCommandCanValidateNativeCustomerWriteUsingProtocolRequest()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var payload = JsonSerializer.Serialize(new GnuCashNativeCustomerWriteRequest(
            bookFixture.BookPath,
            "CUST-1",
            "Test Customer",
            InstallPath: installFixture.InstallPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.ValidateNativeCustomerWrite, payload);
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
        var status = JsonSerializer.Deserialize<GnuCashNativeCustomerWriteStatus>(
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
