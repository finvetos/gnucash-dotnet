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
        new("ping", "Return bridge health information.", "GnuCash.DotNet.Bridge ping [--plain|--json]"),
        new("validate", "Validate the local GnuCash installation.", "GnuCash.DotNet.Bridge validate [--install-path <path>] [--plain|--json]"),
        new("validate-api", "Validate the native GnuCash API surface.", "GnuCash.DotNet.Bridge validate-api [--install-path <path>] [--plain|--json]"),
        new("inventory-exports", "Inventory native exports from the GnuCash install.", "GnuCash.DotNet.Bridge inventory-exports [--install-path <path>] [--all-bin-dlls] [--plain|--json]"),
        new("validate-session", "Open a book read-only through the native GnuCash engine.", "GnuCash.DotNet.Bridge validate-session --book-path <path> [--install-path <path>] [--plain|--json]"),
        new("validate-read-parity", "Compare native core reads with the XML bootstrap reader.", "GnuCash.DotNet.Bridge validate-read-parity --book-path <path> [--install-path <path>] [--plain|--json]"),
        new("validate-write-roundtrip", "Save and reopen a copied book through the native engine.", "GnuCash.DotNet.Bridge validate-write-roundtrip --source-book-path <path> [--working-book-path <path>] [--install-path <path>] [--plain|--json]"),
        new("validate-customer-write", "Create a customer in a copied book.", "GnuCash.DotNet.Bridge validate-customer-write --source-book-path <path> --customer-id <id> --customer-name <name> [--currency-space <space>] [--currency-id <id>] [--plain|--json]"),
        new("headless", "Run the SDK protocol over stdin/stdout.", "GnuCash.DotNet.Bridge headless --stdio")
    ];
}
