using GnuCash.DotNet.Bridge.Discovery;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Inventories native exports from the installed GnuCash runtime.
/// </summary>
public sealed class GnuCashNativeExportInventory
{
    private static readonly string[] DefaultLibraryNames =
    [
        "libgnc-engine.dll",
        "libgnc-module.dll",
        "libgnc-core-utils.dll"
    ];

    private readonly GnuCashInstallationLocator locator;
    private readonly PortableExecutableExportReader exportReader;

    public GnuCashNativeExportInventory()
        : this(new GnuCashInstallationLocator(), new PortableExecutableExportReader())
    {
    }

    internal GnuCashNativeExportInventory(
        GnuCashInstallationLocator locator,
        PortableExecutableExportReader exportReader)
    {
        this.locator = locator;
        this.exportReader = exportReader;
    }

    public GnuCashNativeExportInventoryStatus Inspect(
        string? explicitInstallPath = null,
        bool includeAllBinDlls = false)
    {
        var installation = locator.Validate(explicitInstallPath);
        if (!installation.IsReady || string.IsNullOrWhiteSpace(installation.InstallPath))
        {
            return new GnuCashNativeExportInventoryStatus(
                IsReady: false,
                InstallPath: installation.InstallPath,
                DisplayVersion: installation.DisplayVersion,
                LibraryCount: 0,
                TotalExportCount: 0,
                Libraries: [],
                CheckedPaths: installation.CheckedPaths,
                Message: installation.Message);
        }

        var libraryPaths = GetLibraryPaths(installation.InstallPath, includeAllBinDlls).ToArray();
        var libraries = libraryPaths.Select(ReadLibrary).ToArray();
        var isReady = libraries.All(library => library.IsReady);
        var checkedPaths = installation.CheckedPaths
            .Concat(libraryPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new GnuCashNativeExportInventoryStatus(
            IsReady: isReady,
            InstallPath: installation.InstallPath,
            DisplayVersion: installation.DisplayVersion,
            LibraryCount: libraries.Length,
            TotalExportCount: libraries.Sum(library => library.ExportCount),
            Libraries: libraries,
            CheckedPaths: checkedPaths,
            Message: isReady
                ? "Native export inventory completed."
                : "Native export inventory completed with one or more library inspection errors.");
    }

    private static IEnumerable<string> GetLibraryPaths(string installPath, bool includeAllBinDlls)
    {
        var binPath = Path.Combine(installPath, "bin");
        if (!includeAllBinDlls)
        {
            return DefaultLibraryNames.Select(name => Path.Combine(binPath, name));
        }

        return Directory.EnumerateFiles(binPath, "*.dll", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase);
    }

    private GnuCashNativeLibraryExportInventory ReadLibrary(string libraryPath)
    {
        try
        {
            var exports = exportReader.ReadExportNames(libraryPath)
                .Order(StringComparer.Ordinal)
                .ToArray();
            return new GnuCashNativeLibraryExportInventory(
                Path.GetFileName(libraryPath),
                libraryPath,
                IsReady: true,
                exports.Length,
                exports,
                ErrorMessage: null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new GnuCashNativeLibraryExportInventory(
                Path.GetFileName(libraryPath),
                libraryPath,
                IsReady: false,
                ExportCount: 0,
                Exports: [],
                ErrorMessage: ex.Message);
        }
    }
}
