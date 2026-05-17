namespace GnuCash.DotNet.Bridge.Rendering;

internal static class CliLogo
{
    public static IReadOnlyList<string> GnuCashLines { get; } =
    [
        "  ____                 ____          _     ",
        " / ___|_ __  _   _   / ___|__ _ ___| |__  ",
        "| |  _| '_ \\| | | | | |   / _` / __| '_ \\ ",
        "| |_| | | | | |_| | | |__| (_| \\__ \\ | | |",
        " \\____|_| |_|\\__,_|  \\____\\__,_|___/_| |_|"
    ];

    public static IReadOnlyList<string> DotNetLines { get; } =
    [
        " ____        _   _      _   ",
        "|  _ \\  ___ | |_| \\   / /__| |_ ",
        "| | | |/ _ \\| __|\\ \\ / / _ \\ __|",
        "| |_| | (_) | |_  \\ V /  __/ |_ ",
        "|____/ \\___/ \\__|  \\_/ \\___|\\__|"
    ];

    public static string PlainBanner { get; } =
        string.Join(Environment.NewLine, GnuCashLines.Zip(DotNetLines, (left, right) => left + "-" + right)) +
        Environment.NewLine +
        "GnuCash-DotNet";
}
