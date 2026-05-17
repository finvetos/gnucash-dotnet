using System.CommandLine;
using GnuCash.DotNet.Bridge.Commands;
using GnuCash.DotNet.Bridge.Discovery;
using GnuCash.DotNet.Bridge.Headless;
using GnuCash.DotNet.Bridge.Native;
using GnuCash.DotNet.Bridge.Rendering;
using GnuCash.DotNet.Protocol.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GnuCash.DotNet.Bridge;

/// <summary>
/// Builds and runs the bridge command-line application.
/// </summary>
public sealed class CliApplication
{
    private readonly OutputModeDetector outputModeDetector;
    private readonly RendererFactory rendererFactory;
    private readonly ILogger<CliApplication> logger;

    public CliApplication(
        OutputModeDetector outputModeDetector,
        RendererFactory rendererFactory,
        ILogger<CliApplication> logger)
    {
        this.outputModeDetector = outputModeDetector;
        this.rendererFactory = rendererFactory;
        this.logger = logger;
    }

    public static CliApplication CreateDefault() =>
        new(
            new OutputModeDetector(),
            new RendererFactory(),
            NullLogger<CliApplication>.Instance);

    public async Task<int> RunAsync(
        string[] args,
        TextWriter? output = null,
        TextWriter? error = null,
        bool? isOutputRedirected = null,
        TextReader? input = null)
    {
        ArgumentNullException.ThrowIfNull(args);

        var outputWriter = output ?? Console.Out;
        var errorWriter = error ?? Console.Error;
        var inputReader = input ?? Console.In;
        var mode = outputModeDetector.Detect(
            args,
            isOutputRedirected ?? Console.IsOutputRedirected,
            Environment.GetEnvironmentVariables());
        var renderer = rendererFactory.Create(mode, outputWriter);

        try
        {
            var root = BuildRootCommand(renderer, inputReader, outputWriter, errorWriter);
            var parseResult = root.Parse(args);
            logger.LogDebug("Invoking bridge with output mode {OutputMode}.", mode);
            return await parseResult.InvokeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            errorWriter.WriteLine(ex.Message);
            return 2;
        }
    }

    public RootCommand BuildRootCommand(
        ICliRenderer renderer,
        TextReader input,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var root = new RootCommand("GnuCash.DotNet.Bridge helper process.");
        AddOutputOptions(root);

        root.Subcommands.Add(BuildQuestionCommand(renderer));
        root.Subcommands.Add(BuildHelpCommand(renderer));
        root.Subcommands.Add(BuildListCommand(renderer));
        root.Subcommands.Add(BuildPingCommand(renderer));
        root.Subcommands.Add(BuildValidateCommand(renderer));
        root.Subcommands.Add(BuildValidateNativeApiCommand(renderer));
        root.Subcommands.Add(BuildHeadlessCommand(input, output, error));

        return root;
    }

    private static Command BuildQuestionCommand(ICliRenderer renderer)
    {
        var command = new Command("?", "Show short help.");
        AddOutputOptions(command);
        command.SetAction(_ =>
        {
            renderer.WriteHelp(CommandCatalog.Standard);
            return 0;
        });

        return command;
    }

    private static Command BuildHelpCommand(ICliRenderer renderer)
    {
        var command = new Command("help", "Show short help.");
        AddOutputOptions(command);
        command.SetAction(_ =>
        {
            renderer.WriteHelp(CommandCatalog.Standard);
            return 0;
        });

        return command;
    }

    private static Command BuildListCommand(ICliRenderer renderer)
    {
        var command = new Command("list", "List available commands.");
        AddOutputOptions(command);
        command.SetAction(_ =>
        {
            renderer.WriteCommandList(CommandCatalog.Standard);
            return 0;
        });

        return command;
    }

    private static Command BuildPingCommand(ICliRenderer renderer)
    {
        var command = new Command("ping", "Return bridge health information.");
        AddOutputOptions(command);
        command.SetAction(_ =>
        {
            renderer.WriteJson(BridgeRequestProcessor.CreateHandshake());
            return 0;
        });

        return command;
    }

    private static Command BuildValidateCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path")
        {
            Description = "Validate a specific GnuCash installation path."
        };
        var command = new Command("validate", "Validate the local GnuCash installation.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.SetAction(parseResult =>
        {
            var installPath = parseResult.GetValue(installPathOption);
            var status = new GnuCashInstallationLocator().Validate(installPath);
            renderer.WriteGnuCashValidation(status);
            return status.IsReady ? 0 : 1;
        });

        return command;
    }

    private static Command BuildValidateNativeApiCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path")
        {
            Description = "Validate a specific GnuCash installation path."
        };
        var command = new Command("validate-api", "Validate the native GnuCash API surface.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.SetAction(parseResult =>
        {
            var installPath = parseResult.GetValue(installPathOption);
            var status = new GnuCashNativeApiProbe().Validate(installPath);
            renderer.WriteNativeApiValidation(status);
            return status.IsReady ? 0 : 1;
        });

        return command;
    }

    private static Command BuildHeadlessCommand(TextReader input, TextWriter output, TextWriter error)
    {
        var command = new Command("headless", "Run the SDK protocol over stdin/stdout.");
        command.Options.Add(new Option<bool>("--stdio")
        {
            Description = "Use newline-delimited JSON over standard input and standard output."
        });
        command.SetAction(_ =>
        {
            var session = new HeadlessBridgeSession(new BridgeRequestProcessor());
            return session.RunAsync(input, output, error).GetAwaiter().GetResult();
        });

        return command;
    }

    private static void AddOutputOptions(Command command)
    {
        command.Options.Add(new Option<bool>("--plain")
        {
            Description = "Use plain ASCII output."
        });
        command.Options.Add(new Option<bool>("--json")
        {
            Description = "Emit JSON output where the command supports it."
        });
        command.Options.Add(new Option<string>("--format")
        {
            Description = "Output format: auto, rich, plain, or json.",
            DefaultValueFactory = _ => "auto"
        });
    }
}
