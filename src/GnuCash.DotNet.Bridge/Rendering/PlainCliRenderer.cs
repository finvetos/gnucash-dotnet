using System.Text.Json;
using GnuCash.DotNet.Bridge.Commands;

namespace GnuCash.DotNet.Bridge.Rendering;

public sealed class PlainCliRenderer : ICliRenderer
{
    private readonly TextWriter writer;
    private readonly bool json;

    public PlainCliRenderer(TextWriter writer, bool json = false)
    {
        this.writer = writer;
        this.json = json;
    }

    public void WriteHelp(IReadOnlyList<CommandDescriptor> commands)
    {
        if (json)
        {
            WriteJson(new { name = "GnuCash.DotNet.Bridge", commands });
            return;
        }

        writer.WriteLine("GnuCash.DotNet.Bridge");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  GnuCash.DotNet.Bridge <command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        foreach (var command in commands)
        {
            writer.WriteLine($"  {command.Name,-8} {command.Description}");
        }

        writer.WriteLine();
        writer.WriteLine("Output:");
        writer.WriteLine("  --plain          Use plain ASCII output.");
        writer.WriteLine("  --json           Emit JSON where supported.");
        writer.WriteLine("  --format <mode>  auto, rich, plain, or json.");
    }

    public void WriteCommandList(IReadOnlyList<CommandDescriptor> commands)
    {
        if (json)
        {
            WriteJson(new { commands });
            return;
        }

        writer.WriteLine("Commands");
        writer.WriteLine("--------");
        foreach (var command in commands)
        {
            writer.WriteLine($"{command.Name,-8} {command.Description}");
            writer.WriteLine($"         {command.Usage}");
        }
    }

    public void WriteJson<T>(T value)
    {
        writer.WriteLine(JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
