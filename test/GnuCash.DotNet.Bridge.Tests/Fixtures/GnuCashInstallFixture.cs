namespace GnuCash.DotNet.Bridge.Tests.Fixtures;

internal sealed class GnuCashInstallFixture : IDisposable
{
    private GnuCashInstallFixture(string installPath)
    {
        InstallPath = installPath;
    }

    public string InstallPath { get; }

    public static GnuCashInstallFixture Create()
    {
        var installPath = Path.Combine(
            Path.GetTempPath(),
            "gnucash-dotnet-tests",
            Guid.NewGuid().ToString("N"));

        CreateFile(installPath, "bin", "gnucash.exe");
        CreateFile(installPath, "bin", "gnucash-cli.exe");
        CreateFile(installPath, "bin", "libgnc-core-utils.dll");
        CreateFile(installPath, "bin", "libgnc-engine.dll");
        CreateFile(installPath, "bin", "libgnc-module.dll");
        Directory.CreateDirectory(Path.Combine(installPath, "etc", "gnucash"));
        Directory.CreateDirectory(Path.Combine(installPath, "lib", "gnucash"));
        Directory.CreateDirectory(Path.Combine(installPath, "share", "gnucash"));

        return new GnuCashInstallFixture(installPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(InstallPath))
        {
            Directory.Delete(InstallPath, recursive: true);
        }
    }

    private static void CreateFile(string root, params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
    }
}
