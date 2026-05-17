using System.Text.Json;
using GnuCash.DotNet.Bridge.Tests.Fixtures;
using GnuCash.DotNet.Protocol.Contracts;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class CliNativeTransactionWriteTests
{
    [Fact]
    public async Task HeadlessCommandCanValidateNativeTransactionWriteUsingProtocolRequest()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var payload = JsonSerializer.Serialize(new GnuCashNativeTransactionWriteRequest(
            bookFixture.BookPath,
            "Coffee",
            new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
            [
                new GnuCashNativeTransactionSplitWriteRequest(
                    "11111111111111111111111111111111",
                    new GnuCashAmountRecord("-450/100", -450, 100)),
                new GnuCashNativeTransactionSplitWriteRequest(
                    "22222222222222222222222222222222",
                    new GnuCashAmountRecord("450/100", 450, 100))
            ],
            InstallPath: installFixture.InstallPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.ValidateNativeTransactionWrite, payload);
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
        var status = JsonSerializer.Deserialize<GnuCashNativeTransactionWriteStatus>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(status);
        Assert.False(status!.IsReady);
        Assert.Equal(Path.GetFullPath(bookFixture.BookPath), status.SourceBookPath);
        Assert.Equal("Coffee", status.Description);
        Assert.Equal(2, status.SplitCount);
        Assert.Contains("libgnc-engine.dll", status.Message, StringComparison.Ordinal);
    }
}
