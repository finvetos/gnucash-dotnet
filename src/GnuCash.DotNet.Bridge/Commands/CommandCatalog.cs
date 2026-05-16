namespace GnuCash.DotNet.Bridge.Commands;

/// <summary>
/// Standard command catalog used by the built-in list/help commands.
/// </summary>
public static class CommandCatalog
{
    public static IReadOnlyList<CommandDescriptor> Standard { get; } =
    [
        new("?", "Show short help.", "GnuCash.DotNet.Bridge ? [--plain]"),
        new("help", "Show short help.", "GnuCash.DotNet.Bridge help [--plain]"),
        new("list", "List available commands.", "GnuCash.DotNet.Bridge list [--plain|--json]"),
        new("ping", "Return bridge health information.", "GnuCash.DotNet.Bridge ping [--plain|--json]")
    ];
}