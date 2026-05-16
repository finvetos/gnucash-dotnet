using System.ComponentModel.DataAnnotations;

namespace GnuCash.DotNet.Options;

/// <summary>
/// Options for locating and starting the GnuCash bridge process.
/// </summary>
public sealed class GnuCashBridgeOptions
{
    public const string SectionName = "GnuCash";

    /// <summary>
    /// Optional explicit path to an official GnuCash installation.
    /// </summary>
    public string? InstallPath { get; init; }

    /// <summary>
    /// Optional explicit path to the bundled bridge executable.
    /// </summary>
    public string? BridgeExecutablePath { get; init; }

    /// <summary>
    /// Maximum time to wait for the bridge process to become ready.
    /// </summary>
    [Range(1, 120)]
    public int StartupTimeoutSeconds { get; init; } = 15;
}