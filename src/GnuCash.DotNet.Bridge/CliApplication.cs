using System.CommandLine;
using System.Runtime.InteropServices;
using GnuCash.DotNet.Bridge.Commands;
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
        bool? isOutputRedirected = null)
    {
        ArgumentNullException.ThrowIfNull(args);

        var outputWriter = output ?? Console.Out;
        var errorWriter = error ?? Console.Error;
        var mode = outputModeDetector.Detect(
            args,
            isOutputRedirected ?? Console.IsOutputRedirected,
            Environment.GetEnvironmentVariables());
        var renderer = rendererFactory.Create(mode, outputWriter);

        try
        {
            var root = BuildRootCommand(renderer);
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

    public RootCommand BuildRootCommand(ICliRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        var root = new RootCommand("GnuCash.DotNet.Bridge helper process.");
        AddOutputOptions(root);

        root.Subcommands.Add(BuildQuestionCommand(renderer));
        root.Subcommands.Add(BuildHelpCommand(renderer));
        root.Subcommands.Add(BuildListCommand(renderer));
        root.Subcommands.Add(BuildPingCommand(renderer));

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
            var handshake = new BridgeHandshake(
                BridgeProtocol.CurrentVersion,
                typeof(CliApplication).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                RuntimeInformation.ProcessArchitecture.ToString(),
                Environment.GetEnvironmentVariable("GNUCASH_HOME"));

            renderer.WriteJson(handshake);
            return 0;
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