using System.Text.Json;
using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Bridge.Tests.Fixtures;
using GnuCash.DotNet.Protocol.Contracts;
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
        Assert.Contains("GnuCash-DotNet", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("Commands", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("help", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("list", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("ping", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("validate", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("validate-api", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("validate-session", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("headless", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateCommandReportsReadyWhenInstallPathHasRequiredShape()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            ["validate", "--install-path", fixture.InstallPath, "--plain"],
            writer,
            isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("GnuCash-DotNet", output, StringComparison.Ordinal);
        Assert.Contains("Status:  Ready", output, StringComparison.Ordinal);
        Assert.Contains(fixture.InstallPath, output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateApiCommandReportsMissingExportsWhenEngineDllIsNotPortableExecutable()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            ["validate-api", "--install-path", fixture.InstallPath, "--plain"],
            writer,
            isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("GnuCash native API validation", output, StringComparison.Ordinal);
        Assert.Contains("Status:       Not ready", output, StringComparison.Ordinal);
        Assert.Contains("libgnc-engine.dll", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateSessionCommandReportsNativeApiFailureBeforeOpeningBook()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            [
                "validate-session",
                "--install-path",
                installFixture.InstallPath,
                "--book-path",
                bookFixture.BookPath,
                "--plain"
            ],
            writer,
            isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("GnuCash native session validation", output, StringComparison.Ordinal);
        Assert.Contains("Status:       Not ready", output, StringComparison.Ordinal);
        Assert.Contains(bookFixture.BookPath, output, StringComparison.Ordinal);
        Assert.Contains("libgnc-engine.dll", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateCommandTellsUserToInstallGnuCashWhenMissing()
    {
        var installPath = Path.Combine(Path.GetTempPath(), "gnucash-dotnet-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installPath);
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        try
        {
            var exitCode = await app.RunAsync(
                ["validate", "--install-path", installPath, "--plain"],
                writer,
                isOutputRedirected: false);

        var output = writer.ToString();
        Assert.Equal(1, exitCode);
        Assert.Contains("GnuCash-DotNet", output, StringComparison.Ordinal);
        Assert.Contains("Status:  Not ready", output, StringComparison.Ordinal);
            Assert.Contains("Install GnuCash for Windows first", output, StringComparison.Ordinal);
            Assert.Contains("Missing required paths", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public async Task HeadlessCommandCanLocateGnuCashUsingProtocolRequest()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var locatePayload = JsonSerializer.Serialize(new LocateGnuCashRequest(fixture.InstallPath));
        var locate = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.LocateGnuCash, locatePayload);
        var input = new StringReader(JsonSerializer.Serialize(locate) + Environment.NewLine);
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
        var status = JsonSerializer.Deserialize<GnuCashInstallationStatus>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(status);
        Assert.True(status!.IsReady);
        Assert.Equal(fixture.InstallPath, status.InstallPath);
    }

    [Fact]
    public async Task HeadlessCommandCanValidateNativeApiUsingProtocolRequest()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var locatePayload = JsonSerializer.Serialize(new LocateGnuCashRequest(fixture.InstallPath));
        var locate = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.ValidateNativeApi, locatePayload);
        var input = new StringReader(JsonSerializer.Serialize(locate) + Environment.NewLine);
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
        var status = JsonSerializer.Deserialize<GnuCashNativeApiStatus>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(status);
        Assert.False(status!.IsReady);
        Assert.Contains("qof_session_new", status.RequiredExports);
    }

    [Fact]
    public async Task HeadlessCommandCanValidateNativeSessionUsingProtocolRequest()
    {
        using var installFixture = GnuCashInstallFixture.Create();
        using var bookFixture = GnuCashBookFixture.CreateXml();
        var payload = JsonSerializer.Serialize(new GnuCashNativeSessionRequest(
            bookFixture.BookPath,
            installFixture.InstallPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.ValidateNativeSession, payload);
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
        var status = JsonSerializer.Deserialize<GnuCashNativeSessionStatus>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(status);
        Assert.False(status!.IsReady);
        Assert.Equal(bookFixture.BookPath, status.BookPath);
        Assert.Contains("libgnc-engine.dll", status.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeadlessCommandCanOpenBookUsingProtocolRequest()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var payload = JsonSerializer.Serialize(new GnuCashBookRequest(fixture.BookPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.OpenBook, payload);
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
        var summary = JsonSerializer.Deserialize<GnuCashBookSummary>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.NotNull(summary);
        Assert.Equal(3, summary!.AccountCount);
        Assert.Equal(2, summary.SplitCount);
    }

    [Fact]
    public async Task HeadlessCommandCanListTransactionsUsingProtocolRequest()
    {
        using var fixture = GnuCashBookFixture.CreateXml();
        var payload = JsonSerializer.Serialize(new GnuCashBookRequest(fixture.BookPath));
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.ListTransactions, payload);
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
        var transactions = JsonSerializer.Deserialize<IReadOnlyList<GnuCashTransactionRecord>>(
            response!.PayloadJson!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var transaction = Assert.Single(transactions!);
        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.Equal("Opening deposit", transaction.Description);
        Assert.Equal(2, transaction.Splits.Count);
    }

    [Fact]
    public async Task HelpCommandWritesPlainHelp()
    {
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(["help", "--plain"], writer, isOutputRedirected: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("GnuCash-DotNet", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("Usage:", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuestionCommandWritesPlainHelp()
    {
        var writer = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(["?", "--plain"], writer, isOutputRedirected: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("GnuCash-DotNet", writer.ToString(), StringComparison.Ordinal);
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
        Assert.DoesNotContain("GnuCash-DotNet", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeadlessCommandProcessesProtocolRequestsOverStdio()
    {
        var ping = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.Ping);
        var shutdown = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.Shutdown);
        var input = new StringReader(string.Join(
            Environment.NewLine,
            JsonSerializer.Serialize(ping),
            JsonSerializer.Serialize(shutdown),
            string.Empty));
        var output = new StringWriter();
        var error = new StringWriter();
        var app = CliApplication.CreateDefault();

        var exitCode = await app.RunAsync(
            ["headless", "--stdio"],
            output,
            error,
            isOutputRedirected: true,
            input: input);

        var responses = output.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<BridgeResponse>(line))
            .ToArray();

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        Assert.Equal(2, responses.Length);
        Assert.NotNull(responses[0]);
        Assert.Equal(ping.Id, responses[0]!.Id);
        Assert.True(responses[0]!.Succeeded);
        Assert.NotNull(responses[0]!.PayloadJson);
        Assert.Contains("ProcessArchitecture", responses[0]!.PayloadJson, StringComparison.Ordinal);
        Assert.NotNull(responses[1]);
        Assert.Equal(shutdown.Id, responses[1]!.Id);
        Assert.True(responses[1]!.Succeeded);
    }

    [Fact]
    public async Task HeadlessCommandWritesMalformedRequestAsProtocolError()
    {
        var input = new StringReader("not json" + Environment.NewLine);
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

        Assert.Equal(0, exitCode);
        Assert.NotNull(response);
        Assert.False(response!.Succeeded);
        Assert.Equal("MalformedRequest", response.ErrorCode);
        Assert.Contains("Malformed bridge request", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Commands", output.ToString(), StringComparison.Ordinal);
    }

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
            CreateFile(installPath, "bin", "libgnc-module.dll");
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
