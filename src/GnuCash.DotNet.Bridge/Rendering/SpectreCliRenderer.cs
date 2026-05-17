using GnuCash.DotNet.Bridge.Commands;
using GnuCash.DotNet.Protocol.Contracts;
using Spectre.Console;

namespace GnuCash.DotNet.Bridge.Rendering;

public sealed class SpectreCliRenderer : ICliRenderer
{
    private readonly TextWriter writer;
    private readonly IAnsiConsole console;

    public SpectreCliRenderer(TextWriter writer)
    {
        this.writer = writer;
        console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer)
        });
    }

    public void WriteHelp(IReadOnlyList<CommandDescriptor> commands)
    {
        WriteLogo();
        console.Write(
            new Panel("[bold]GnuCash.DotNet.Bridge[/]\n\nUsage: [yellow]GnuCash.DotNet.Bridge <command> [options][/]")
                .Header("Help")
                .Border(BoxBorder.Rounded));

        WriteCommandList(commands);
    }

    public void WriteCommandList(IReadOnlyList<CommandDescriptor> commands)
    {
        WriteLogo();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Command[/]")
            .AddColumn("[bold]Description[/]")
            .AddColumn("[bold]Usage[/]");

        foreach (var command in commands)
        {
            table.AddRow(
                Markup.Escape(command.Name),
                Markup.Escape(command.Description),
                Markup.Escape(command.Usage));
        }

        console.Write(table);
    }

    public void WriteGnuCashValidation(GnuCashInstallationStatus status)
    {
        WriteLogo();
        if (status.IsReady)
        {
            console.Write(new Panel("[bold green]GnuCash is ready[/]\nThe bridge can use the local installation.")
                .Header("Validation")
                .Border(BoxBorder.Rounded));

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Field[/]")
                .AddColumn("[bold]Value[/]");

            table.AddRow("Path", Markup.Escape(status.InstallPath ?? "unknown"));
            table.AddRow("Version", Markup.Escape(status.DisplayVersion ?? "unknown"));
            table.AddRow("Source", Markup.Escape(status.Source ?? "unknown"));
            console.Write(table);
            return;
        }

        console.Write(new Panel("[bold red]GnuCash is not ready[/]\nInstall GnuCash for Windows first, then run validation again.")
            .Header("Validation")
            .Border(BoxBorder.Rounded));

        WriteValues("Checked paths", status.CheckedPaths);
        WriteValues("Missing required paths", status.MissingPaths);
    }

    public void WriteNativeApiValidation(GnuCashNativeApiStatus status)
    {
        WriteLogo();
        var statusMarkup = status.IsReady ? "[bold green]Native API exports are ready[/]" : "[bold red]Native API exports are not ready[/]";
        console.Write(new Panel(statusMarkup + "\n" + Markup.Escape(status.Message))
            .Header("Native API")
            .Border(BoxBorder.Rounded));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Callable now", status.CanCallFromCurrentProcess ? "Yes" : "No");
        table.AddRow("Architecture", Markup.Escape(status.ProcessArchitecture));
        table.AddRow("Path", Markup.Escape(status.InstallPath ?? "unknown"));
        table.AddRow("Engine", Markup.Escape(status.EnginePath ?? "unknown"));
        table.AddRow("Version", Markup.Escape(status.DisplayVersion ?? "unknown"));
        console.Write(table);

        WriteValues("Missing native exports", status.MissingExports);
    }

    public void WriteNativeExportInventory(GnuCashNativeExportInventoryStatus status)
    {
        WriteLogo();
        var statusMarkup = status.IsReady ? "[bold green]Export inventory is ready[/]" : "[bold red]Export inventory has errors[/]";
        console.Write(new Panel(statusMarkup + "\n" + Markup.Escape(status.Message))
            .Header("Native Exports")
            .Border(BoxBorder.Rounded));

        var summary = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Value[/]");
        summary.AddRow("Path", Markup.Escape(status.InstallPath ?? "unknown"));
        summary.AddRow("Version", Markup.Escape(status.DisplayVersion ?? "unknown"));
        summary.AddRow("Libraries", status.LibraryCount.ToString());
        summary.AddRow("Exports", status.TotalExportCount.ToString());
        console.Write(summary);

        var libraries = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Library[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Exports[/]")
            .AddColumn("[bold]Error[/]");
        foreach (var library in status.Libraries)
        {
            libraries.AddRow(
                Markup.Escape(library.Name),
                library.IsReady ? "Ready" : "Not ready",
                library.ExportCount.ToString(),
                Markup.Escape(library.ErrorMessage ?? ""));
        }

        console.Write(libraries);
    }

    public void WriteNativeSessionValidation(GnuCashNativeSessionStatus status)
    {
        WriteLogo();
        var statusMarkup = status.IsReady ? "[bold green]Native session is ready[/]" : "[bold red]Native session is not ready[/]";
        console.Write(new Panel(statusMarkup + "\n" + Markup.Escape(status.Message))
            .Header("Native Session")
            .Border(BoxBorder.Rounded));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Callable now", status.CanCallFromCurrentProcess ? "Yes" : "No");
        table.AddRow("Architecture", Markup.Escape(status.ProcessArchitecture));
        table.AddRow("Book", Markup.Escape(status.BookPath));
        table.AddRow("Path", Markup.Escape(status.InstallPath ?? "unknown"));
        table.AddRow("Engine", Markup.Escape(status.EnginePath ?? "unknown"));
        table.AddRow("Version", Markup.Escape(status.DisplayVersion ?? "unknown"));
        table.AddRow("Session file", Markup.Escape(status.SessionFilePath ?? "unknown"));
        table.AddRow("Session URL", Markup.Escape(status.SessionUrl ?? "unknown"));
        table.AddRow("Has book", status.HasBook ? "Yes" : "No");
        table.AddRow("Has root", status.HasRootAccount ? "Yes" : "No");
        table.AddRow("Accounts", status.AccountCount?.ToString() ?? "unknown");
        table.AddRow("Commodities", status.CommodityCount?.ToString() ?? "unknown");
        table.AddRow("Transactions", status.TransactionCount?.ToString() ?? "unknown");
        table.AddRow("Backend error", status.BackendErrorCode?.ToString() ?? "none");
        table.AddRow("Backend message", Markup.Escape(status.BackendErrorMessage ?? "none"));
        console.Write(table);
    }

    public void WriteNativeReadParityValidation(GnuCashNativeReadParityStatus status)
    {
        WriteLogo();
        var statusMarkup = status.IsReady ? "[bold green]Native read parity is ready[/]" : "[bold red]Native read parity is not ready[/]";
        console.Write(new Panel(statusMarkup + "\n" + Markup.Escape(status.Message))
            .Header("Native Read Parity")
            .Border(BoxBorder.Rounded));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Callable now", status.CanCallFromCurrentProcess ? "Yes" : "No");
        table.AddRow("Architecture", Markup.Escape(status.ProcessArchitecture));
        table.AddRow("Book", Markup.Escape(status.BookPath));
        table.AddRow("Path", Markup.Escape(status.InstallPath ?? "unknown"));
        table.AddRow("Engine", Markup.Escape(status.EnginePath ?? "unknown"));
        table.AddRow("Version", Markup.Escape(status.DisplayVersion ?? "unknown"));
        table.AddRow("Native book", Markup.Escape(status.NativeBookId ?? "unknown"));
        table.AddRow("XML book", Markup.Escape(status.XmlBookId ?? "unknown"));
        table.AddRow("Native accounts", status.NativeAccountCount?.ToString() ?? "unknown");
        table.AddRow("XML accounts", status.XmlAccountCount?.ToString() ?? "unknown");
        table.AddRow("Native commodities", status.NativeCommodityCount?.ToString() ?? "unknown");
        table.AddRow("XML commodities", status.XmlCommodityCount?.ToString() ?? "unknown");
        table.AddRow("Native transactions", status.NativeTransactionCount?.ToString() ?? "unknown");
        table.AddRow("XML transactions", status.XmlTransactionCount?.ToString() ?? "unknown");
        table.AddRow("Native splits", status.NativeSplitCount?.ToString() ?? "unknown");
        table.AddRow("XML splits", status.XmlSplitCount?.ToString() ?? "unknown");
        table.AddRow("Native prices", status.NativePriceCount?.ToString() ?? "unknown");
        table.AddRow("XML prices", status.XmlPriceCount?.ToString() ?? "unknown");
        console.Write(table);

        WriteValues("Book mismatches", status.BookMismatches);
        WriteValues("Account mismatches", status.AccountMismatches);
        WriteValues("Commodity mismatches", status.CommodityMismatches);
        WriteValues("Transaction mismatches", status.TransactionMismatches);
        WriteValues("Price mismatches", status.PriceMismatches);
    }

    public void WriteNativeWriteRoundTripValidation(GnuCashNativeWriteRoundTripStatus status)
    {
        WriteLogo();
        var statusMarkup = status.IsReady ? "[bold green]Native write round-trip is ready[/]" : "[bold red]Native write round-trip is not ready[/]";
        console.Write(new Panel(statusMarkup + "\n" + Markup.Escape(status.Message))
            .Header("Native Write Round-Trip")
            .Border(BoxBorder.Rounded));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Callable now", status.CanCallFromCurrentProcess ? "Yes" : "No");
        table.AddRow("Architecture", Markup.Escape(status.ProcessArchitecture));
        table.AddRow("Source", Markup.Escape(status.SourceBookPath));
        table.AddRow("Working", Markup.Escape(status.WorkingBookPath));
        table.AddRow("Path", Markup.Escape(status.InstallPath ?? "unknown"));
        table.AddRow("Engine", Markup.Escape(status.EnginePath ?? "unknown"));
        table.AddRow("Version", Markup.Escape(status.DisplayVersion ?? "unknown"));
        table.AddRow("Before accounts", status.BeforeAccountCount?.ToString() ?? "unknown");
        table.AddRow("After accounts", status.AfterAccountCount?.ToString() ?? "unknown");
        table.AddRow("Before commodities", status.BeforeCommodityCount?.ToString() ?? "unknown");
        table.AddRow("After commodities", status.AfterCommodityCount?.ToString() ?? "unknown");
        table.AddRow("Before transactions", status.BeforeTransactionCount?.ToString() ?? "unknown");
        table.AddRow("After transactions", status.AfterTransactionCount?.ToString() ?? "unknown");
        table.AddRow("Backend error", status.BackendErrorCode?.ToString() ?? "none");
        table.AddRow("Backend message", Markup.Escape(status.BackendErrorMessage ?? "none"));
        console.Write(table);
    }

    public void WriteNativeCustomerWriteValidation(GnuCashNativeCustomerWriteStatus status)
    {
        WriteLogo();
        var statusMarkup = status.IsReady ? "[bold green]Native customer write is ready[/]" : "[bold red]Native customer write is not ready[/]";
        console.Write(new Panel(statusMarkup + "\n" + Markup.Escape(status.Message))
            .Header("Native Customer Write")
            .Border(BoxBorder.Rounded));

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Field[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Callable now", status.CanCallFromCurrentProcess ? "Yes" : "No");
        table.AddRow("Architecture", Markup.Escape(status.ProcessArchitecture));
        table.AddRow("Source", Markup.Escape(status.SourceBookPath));
        table.AddRow("Working", Markup.Escape(status.WorkingBookPath));
        table.AddRow("Customer", Markup.Escape(status.CustomerId + " - " + status.CustomerName));
        table.AddRow("Currency", Markup.Escape(status.CurrencySpace + "::" + status.CurrencyId));
        table.AddRow("Guid", Markup.Escape(status.CreatedCustomerGuid ?? "unknown"));
        table.AddRow("Before customers", status.BeforeCustomerCount?.ToString() ?? "unknown");
        table.AddRow("After customers", status.AfterCustomerCount?.ToString() ?? "unknown");
        table.AddRow("Found after reopen", status.FoundAfterReopen ? "Yes" : "No");
        table.AddRow("Backend error", status.BackendErrorCode?.ToString() ?? "none");
        table.AddRow("Backend message", Markup.Escape(status.BackendErrorMessage ?? "none"));
        console.Write(table);
    }

    public void WriteJson<T>(T value)
    {
        new PlainCliRenderer(writer, json: true).WriteJson(value);
    }

    private void WriteValues(string title, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]" + Markup.Escape(title) + "[/]");

        foreach (var value in values)
        {
            table.AddRow(Markup.Escape(value));
        }

        console.Write(table);
    }

    private void WriteLogo()
    {
        for (var i = 0; i < CliLogo.GnuCashLines.Count; i++)
        {
            console.Markup("[bold lime]" + Markup.Escape(CliLogo.GnuCashLines[i]) + "[/]");
            console.Markup("[bold yellow]-" + Markup.Escape(CliLogo.DotNetLines[i]) + "[/]");
            console.WriteLine();
        }

        console.MarkupLine("[bold lime]GnuCash[/][bold yellow]-DotNet[/]");
        console.WriteLine();
    }
}
