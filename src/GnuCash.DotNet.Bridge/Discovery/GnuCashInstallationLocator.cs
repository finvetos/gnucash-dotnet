using System.Diagnostics;
using System.Security;
using GnuCash.DotNet.Protocol.Contracts;
using Microsoft.Win32;

namespace GnuCash.DotNet.Bridge.Discovery;

/// <summary>
/// Locates and validates an official GnuCash installation for the bridge process.
/// </summary>
public sealed class GnuCashInstallationLocator
{
    private static readonly string[] RequiredRelativePaths =
    [
        Path.Combine("bin", "gnucash.exe"),
        Path.Combine("bin", "gnucash-cli.exe"),
        Path.Combine("bin", "libgnc-core-utils.dll"),
        Path.Combine("bin", "libgnc-engine.dll"),
        Path.Combine("etc", "gnucash"),
        Path.Combine("lib", "gnucash"),
        Path.Combine("share", "gnucash")
    ];

    public GnuCashInstallationStatus Validate(string? explicitInstallPath = null)
    {
        var checkedPaths = new List<string>();
        var candidates = GetCandidates(explicitInstallPath)
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.InstallPath))
            .DistinctBy(candidate => NormalizePath(candidate.InstallPath), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var candidate in candidates)
        {
            var installPath = NormalizePath(candidate.InstallPath);
            checkedPaths.Add(installPath);

            var missingPaths = GetMissingRequiredPaths(installPath);
            if (missingPaths.Count == 0)
            {
                return new GnuCashInstallationStatus(
                    IsReady: true,
                    InstallPath: installPath,
                    DisplayVersion: candidate.DisplayVersion ?? TryReadFileVersion(installPath),
                    Source: candidate.Source,
                    MissingPaths: [],
                    CheckedPaths: checkedPaths,
                    Message: "GnuCash is installed and the bridge can use it.");
            }

            if (!string.IsNullOrWhiteSpace(explicitInstallPath))
            {
                return NotReady(checkedPaths, missingPaths);
            }
        }

        return NotReady(checkedPaths, []);
    }

    private static GnuCashInstallationStatus NotReady(
        IReadOnlyList<string> checkedPaths,
        IReadOnlyList<string> missingPaths) =>
        new(
            IsReady: false,
            InstallPath: null,
            DisplayVersion: null,
            Source: null,
            MissingPaths: missingPaths,
            CheckedPaths: checkedPaths,
            Message: "GnuCash is not installed or the installation is incomplete. Install GnuCash for Windows first, then run validation again.");

    private static IReadOnlyList<string> GetMissingRequiredPaths(string installPath) =>
        RequiredRelativePaths
            .Select(relativePath => Path.Combine(installPath, relativePath))
            .Where(path => !File.Exists(path) && !Directory.Exists(path))
            .ToArray();

    private static IEnumerable<GnuCashInstallCandidate> GetCandidates(string? explicitInstallPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitInstallPath))
        {
            yield return new GnuCashInstallCandidate(explicitInstallPath, "command line", null);
            yield break;
        }

        var environmentPath = Environment.GetEnvironmentVariable("GNUCASH_HOME");
        if (!string.IsNullOrWhiteSpace(environmentPath))
        {
            yield return new GnuCashInstallCandidate(environmentPath, "GNUCASH_HOME", null);
        }

        foreach (var registryCandidate in ReadRegistryCandidates())
        {
            yield return registryCandidate;
        }

        foreach (var commonPath in ReadCommonInstallPaths())
        {
            yield return new GnuCashInstallCandidate(commonPath, "common install path", null);
        }
    }

    private static IEnumerable<GnuCashInstallCandidate> ReadRegistryCandidates()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        foreach (var registryView in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
            using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                GnuCashInstallCandidate? candidate = null;
                try
                {
                    using var subKey = uninstallKey.OpenSubKey(subKeyName);
                    var displayName = subKey?.GetValue("DisplayName") as string;
                    var installLocation = subKey?.GetValue("InstallLocation") as string;
                    var displayVersion = subKey?.GetValue("DisplayVersion") as string;

                    if (displayName?.StartsWith("GnuCash", StringComparison.OrdinalIgnoreCase) == true &&
                        !string.IsNullOrWhiteSpace(installLocation))
                    {
                        candidate = new GnuCashInstallCandidate(
                            installLocation,
                            $"registry {registryView}",
                            displayVersion);
                    }
                }
                catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (candidate is not null)
                {
                    yield return candidate;
                }
            }
        }
    }

    private static IEnumerable<string> ReadCommonInstallPaths()
    {
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "gnucash");
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "gnucash");
        }
    }

    private static string? TryReadFileVersion(string installPath)
    {
        var executablePath = Path.Combine(installPath, "bin", "gnucash.exe");
        if (!File.Exists(executablePath))
        {
            return null;
        }

        var version = FileVersionInfo.GetVersionInfo(executablePath).ProductVersion;
        return string.IsNullOrWhiteSpace(version) ? null : version;
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private sealed record GnuCashInstallCandidate(
        string InstallPath,
        string Source,
        string? DisplayVersion);
}
