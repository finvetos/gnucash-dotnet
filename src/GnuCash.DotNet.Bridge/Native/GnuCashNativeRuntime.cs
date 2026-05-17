using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

public sealed class GnuCashNativeRuntime
{
    private static int dllResolverConfigured;

    private readonly GnuCashNativeApiProbe apiProbe;

    public GnuCashNativeRuntime()
        : this(new GnuCashNativeApiProbe())
    {
    }

    internal GnuCashNativeRuntime(GnuCashNativeApiProbe apiProbe)
    {
        this.apiProbe = apiProbe;
    }

    public GnuCashNativeRuntimeState Prepare(string? explicitInstallPath = null)
    {
        var apiStatus = apiProbe.Validate(explicitInstallPath);
        if (!apiStatus.IsReady ||
            !apiStatus.CanCallFromCurrentProcess ||
            string.IsNullOrWhiteSpace(apiStatus.InstallPath) ||
            string.IsNullOrWhiteSpace(apiStatus.EnginePath))
        {
            return new GnuCashNativeRuntimeState(apiStatus, IsPrepared: false);
        }

        ConfigureProcessEnvironment(apiStatus.InstallPath);
        _ = GnuCashNativeMethods.gnc_gbr_init(IntPtr.Zero);
        GnuCashNativeMethods.gnc_environment_setup();
        GnuCashNativeMethods.gnc_module_system_init();
        GnuCashNativeMethods.gnc_engine_init(0, IntPtr.Zero);
        return new GnuCashNativeRuntimeState(apiStatus, IsPrepared: true);
    }

    private static void ConfigureProcessEnvironment(string installPath)
    {
        var binPath = Path.Combine(installPath, "bin");
        var libPath = Path.Combine(installPath, "lib", "gnucash");
        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        if (!existingPath.Split(Path.PathSeparator).Contains(binPath, StringComparer.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("PATH", binPath + Path.PathSeparator + existingPath);
        }

        Environment.SetEnvironmentVariable("GNUCASH_HOME", installPath);
        Environment.SetEnvironmentVariable("GNC_UNINSTALLED", "1");
        Environment.SetEnvironmentVariable("GNC_BUILDDIR", installPath);
        Environment.SetEnvironmentVariable("GNC_MODULE_PATH", binPath + Path.PathSeparator + libPath);

        if (OperatingSystem.IsWindows() && Interlocked.Exchange(ref dllResolverConfigured, 1) == 0)
        {
            NativeLibrary.SetDllImportResolver(
                typeof(GnuCashNativeRuntime).Assembly,
                (libraryName, assembly, searchPath) =>
                {
                    var candidate = Path.Combine(binPath, libraryName);
                    return File.Exists(candidate)
                        ? NativeLibrary.Load(candidate, assembly, searchPath)
                        : IntPtr.Zero;
                });
        }
    }
}

public sealed record GnuCashNativeRuntimeState(
    GnuCashNativeApiStatus ApiStatus,
    bool IsPrepared);
