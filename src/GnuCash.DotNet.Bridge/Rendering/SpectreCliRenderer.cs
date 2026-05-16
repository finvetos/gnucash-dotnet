using GnuCash.DotNet.Bridge.Commands;
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
        console.Write(
            new Panel("[bold]GnuCash.DotNet.Bridge[/]\n\nUsage: [yellow]GnuCash.DotNet.Bridge <command> [options][/]")
                .Header("Help")
                .Border(BoxBorder.Rounded));

        WriteCommandList(commands);
    }

    public void WriteCommandList(IReadOnlyList<CommandDescriptor> commands)
    {
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

    public void WriteJson<T>(T value)
    {
        new PlainCliRenderer(writer, json: true).WriteJson(value);
    }
}