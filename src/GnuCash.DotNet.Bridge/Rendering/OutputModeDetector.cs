using System.Collections;

namespace GnuCash.DotNet.Bridge.Rendering;

public sealed class OutputModeDetector
{
    public OutputMode Detect(string[] args, bool isOutputRedirected, IDictionary environment)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(environment);

        var explicitFormat = ReadOption(args, "--format");
        if (HasFlag(args, "--json") || IsFormat(explicitFormat, "json"))
        {
            return OutputMode.Json;
        }

        if (HasFlag(args, "--plain") ||
            IsFormat(explicitFormat, "plain") ||
            IsFormat(explicitFormat, "ascii") ||
            IsFormat(explicitFormat, "text") ||
            isOutputRedirected ||
            IsNoColor(environment) ||
            IsDumbTerminal(environment))
        {
            return OutputMode.Plain;
        }

        return OutputMode.Rich;
    }

    private static bool HasFlag(IReadOnlyList<string> args, string name) =>
        args.Any(arg => string.Equals(arg, name, StringComparison.Ordinal));

    private static bool IsFormat(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsNoColor(IDictionary environment) =>
        environment.Contains("NO_COLOR");

    private static bool IsDumbTerminal(IDictionary environment) =>
        string.Equals(environment["TERM"] as string, "dumb", StringComparison.OrdinalIgnoreCase);

    private static string? ReadOption(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return i + 1 < args.Count ? args[i + 1] : null;
            }

            var prefix = name + "=";
            if (args[i].StartsWith(prefix, StringComparison.Ordinal))
            {
                return args[i][prefix.Length..];
            }
        }

        return null;
    }
}
