using GnuCash.DotNet.Bridge;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task ListCommandWritesPlainCommandCatalog()
    {
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(["list", "--plain"], writer, isOutputRedirected: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("Commands", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("help", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("list", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("ping", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HelpCommandWritesPlainHelp()
    {
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(["help", "--plain"], writer, isOutputRedirected: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuestionCommandWritesPlainHelp()
    {
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(["?", "--plain"], writer, isOutputRedirected: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PingCommandWritesBridgeHandshakeJson()
    {
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(["ping", "--json"], writer, isOutputRedirected: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("ProtocolVersion", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("ProcessArchitecture", writer.ToString(), StringComparison.Ordinal);
    }
}