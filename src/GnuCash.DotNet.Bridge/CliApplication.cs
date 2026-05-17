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
        root.Subcommands.Add(BuildInventoryExportsCommand(renderer));
        root.Subcommands.Add(BuildValidateNativeSessionCommand(renderer));
        root.Subcommands.Add(BuildValidateNativeReadParityCommand(renderer));
        root.Subcommands.Add(BuildValidateNativeWriteRoundTripCommand(renderer));
        root.Subcommands.Add(BuildValidateNativeCustomerWriteCommand(renderer));
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

    private static Command BuildInventoryExportsCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path")
        {
            Description = "Inspect a specific GnuCash installation path."
        };
        var allBinDllsOption = new Option<bool>("--all-bin-dlls")
        {
            Description = "Inventory every DLL directly under the GnuCash bin directory."
        };
        var command = new Command("inventory-exports", "Inventory native exports from the GnuCash install.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.Options.Add(allBinDllsOption);
        command.SetAction(parseResult =>
        {
            var status = new GnuCashNativeExportInventory().Inspect(
                parseResult.GetValue(installPathOption),
                parseResult.GetValue(allBinDllsOption));
            renderer.WriteNativeExportInventory(status);
            return status.IsReady ? 0 : 1;
        });

        return command;
    }

    private static Command BuildValidateNativeSessionCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path")
        {
            Description = "Validate a specific GnuCash installation path."
        };
        var bookPathOption = new Option<string?>("--book-path")
        {
            Description = "Open this GnuCash book read-only through the native engine."
        };
        var command = new Command("validate-session", "Open a book read-only through the native GnuCash engine.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.Options.Add(bookPathOption);
        command.SetAction(parseResult =>
        {
            var bookPath = parseResult.GetValue(bookPathOption);
            if (string.IsNullOrWhiteSpace(bookPath))
            {
                throw new ArgumentException("A --book-path value is required.");
            }

            var installPath = parseResult.GetValue(installPathOption);
            var status = new GnuCashNativeSession().ValidateReadOnlyOpen(bookPath, installPath);
            renderer.WriteNativeSessionValidation(status);
            return status.IsReady ? 0 : 1;
        });

        return command;
    }

    private static Command BuildValidateNativeReadParityCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path")
        {
            Description = "Validate a specific GnuCash installation path."
        };
        var bookPathOption = new Option<string?>("--book-path")
        {
            Description = "Compare native reads for this GnuCash book against the XML reader."
        };
        var command = new Command("validate-read-parity", "Compare native core book reads.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.Options.Add(bookPathOption);
        command.SetAction(parseResult =>
        {
            var bookPath = parseResult.GetValue(bookPathOption);
            if (string.IsNullOrWhiteSpace(bookPath))
            {
                throw new ArgumentException("A --book-path value is required.");
            }

            var installPath = parseResult.GetValue(installPathOption);
            var status = new GnuCashNativeReadParityChecker().Validate(bookPath, installPath);
            renderer.WriteNativeReadParityValidation(status);
            return status.IsReady ? 0 : 1;
        });

        return command;
    }

    private static Command BuildValidateNativeWriteRoundTripCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path")
        {
            Description = "Validate a specific GnuCash installation path."
        };
        var sourceBookPathOption = new Option<string?>("--source-book-path")
        {
            Description = "Copy this GnuCash book, save the copy, and reopen it."
        };
        var workingBookPathOption = new Option<string?>("--working-book-path")
        {
            Description = "Optional destination for the copied validation book."
        };
        var command = new Command("validate-write-roundtrip", "Save and reopen a copied book through the native engine.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.Options.Add(sourceBookPathOption);
        command.Options.Add(workingBookPathOption);
        command.SetAction(parseResult =>
        {
            var sourceBookPath = parseResult.GetValue(sourceBookPathOption);
            if (string.IsNullOrWhiteSpace(sourceBookPath))
            {
                throw new ArgumentException("A --source-book-path value is required.");
            }

            var status = new GnuCashNativeWriteRoundTripValidator().Validate(
                sourceBookPath,
                parseResult.GetValue(workingBookPathOption),
                parseResult.GetValue(installPathOption));
            renderer.WriteNativeWriteRoundTripValidation(status);
            return status.IsReady ? 0 : 1;
        });

        return command;
    }

    private static Command BuildValidateNativeCustomerWriteCommand(ICliRenderer renderer)
    {
        var installPathOption = new Option<string?>("--install-path");
        var sourceBookPathOption = new Option<string?>("--source-book-path");
        var workingBookPathOption = new Option<string?>("--working-book-path");
        var customerIdOption = new Option<string?>("--customer-id");
        var customerNameOption = new Option<string?>("--customer-name");
        var currencySpaceOption = new Option<string>("--currency-space")
        {
            DefaultValueFactory = _ => "CURRENCY"
        };
        var currencyIdOption = new Option<string>("--currency-id")
        {
            DefaultValueFactory = _ => "USD"
        };
        var command = new Command("validate-customer-write", "Create a customer in a copied book.");
        AddOutputOptions(command);
        command.Options.Add(installPathOption);
        command.Options.Add(sourceBookPathOption);
        command.Options.Add(workingBookPathOption);
        command.Options.Add(customerIdOption);
        command.Options.Add(customerNameOption);
        command.Options.Add(currencySpaceOption);
        command.Options.Add(currencyIdOption);
        command.SetAction(parseResult =>
        {
            var sourceBookPath = parseResult.GetValue(sourceBookPathOption);
            var customerId = parseResult.GetValue(customerIdOption);
            var customerName = parseResult.GetValue(customerNameOption);
            if (string.IsNullOrWhiteSpace(sourceBookPath) ||
                string.IsNullOrWhiteSpace(customerId) ||
                string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException(
                    "--source-book-path, --customer-id, and --customer-name values are required.");
            }

            var request = new GnuCashNativeCustomerWriteRequest(
                sourceBookPath,
                customerId,
                customerName,
                parseResult.GetValue(currencySpaceOption) ?? "CURRENCY",
                parseResult.GetValue(currencyIdOption) ?? "USD",
                parseResult.GetValue(workingBookPathOption),
                parseResult.GetValue(installPathOption));
            var status = new GnuCashNativeCustomerWriteValidator().Validate(request);
            renderer.WriteNativeCustomerWriteValidation(status);
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
