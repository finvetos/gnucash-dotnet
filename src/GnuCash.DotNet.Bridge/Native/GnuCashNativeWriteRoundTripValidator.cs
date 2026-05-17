using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Validates that a copied book can be saved and reopened through the native engine.
/// </summary>
public sealed class GnuCashNativeWriteRoundTripValidator
{
    private readonly GnuCashNativeRuntime runtime;

    public GnuCashNativeWriteRoundTripValidator()
        : this(new GnuCashNativeRuntime())
    {
    }

    internal GnuCashNativeWriteRoundTripValidator(GnuCashNativeRuntime runtime)
    {
        this.runtime = runtime;
    }

    public GnuCashNativeWriteRoundTripStatus Validate(
        string sourceBookPath,
        string? workingBookPath = null,
        string? explicitInstallPath = null)
    {
        var paths = ResolvePaths(sourceBookPath, workingBookPath);
        if (!paths.IsReady)
        {
            return CreateStatus(null, paths, paths.Message);
        }

        var runtimeState = runtime.Prepare(explicitInstallPath);
        if (!runtimeState.IsPrepared)
        {
            return CreateStatus(runtimeState.ApiStatus, paths, runtimeState.ApiStatus.Message);
        }

        return ValidatePrepared(runtimeState.ApiStatus, paths);
    }

    private static GnuCashNativeWriteRoundTripStatus ValidatePrepared(
        GnuCashNativeApiStatus apiStatus,
        GnuCashWriteRoundTripPaths paths)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(paths.WorkingBookPath)!);
            File.Copy(paths.SourceBookPath, paths.WorkingBookPath, overwrite: false);

            using var writable = GnuCashNativeSessionHandle.OpenWritable(paths.WorkingBookPath);
            var before = GnuCashNativeSessionCounts.From(writable);
            writable.Save();

            using var reopened = GnuCashNativeSessionHandle.OpenReadOnly(paths.WorkingBookPath);
            var after = GnuCashNativeSessionCounts.From(reopened);
            return CreateLoadedStatus(apiStatus, paths, before, after);
        }
        catch (GnuCashNativeOperationException ex)
        {
            return CreateStatus(
                apiStatus,
                paths,
                ex.Message,
                backendErrorCode: ex.BackendErrorCode,
                backendErrorMessage: ex.BackendErrorMessage);
        }
        catch (Exception ex) when (
            ex is IOException or
                  UnauthorizedAccessException or
                  DllNotFoundException or
                  EntryPointNotFoundException or
                  BadImageFormatException or
                  SEHException or
                  InvalidOperationException)
        {
            return CreateStatus(apiStatus, paths, "The native write round-trip failed: " + ex.Message);
        }
    }

    private static GnuCashWriteRoundTripPaths ResolvePaths(
        string sourceBookPath,
        string? workingBookPath)
    {
        if (string.IsNullOrWhiteSpace(sourceBookPath))
        {
            return GnuCashWriteRoundTripPaths.Failed(string.Empty, string.Empty, "A source book path is required.");
        }

        var source = Path.GetFullPath(sourceBookPath);
        if (!File.Exists(source))
        {
            return GnuCashWriteRoundTripPaths.Failed(source, string.Empty, "The source GnuCash book does not exist.");
        }

        var working = string.IsNullOrWhiteSpace(workingBookPath)
            ? CreateDefaultWorkingPath(source)
            : Path.GetFullPath(workingBookPath);
        return File.Exists(working)
            ? GnuCashWriteRoundTripPaths.Failed(source, working, "The working book path already exists.")
            : GnuCashWriteRoundTripPaths.Ready(source, working);
    }

    private static string CreateDefaultWorkingPath(string sourceBookPath)
    {
        var extension = Path.GetExtension(sourceBookPath);
        var fileName = "roundtrip-" + Guid.NewGuid().ToString("N") + extension;
        return Path.Combine(Path.GetTempPath(), "gnucash-dotnet-write-roundtrip", fileName);
    }

    private static GnuCashNativeWriteRoundTripStatus CreateLoadedStatus(
        GnuCashNativeApiStatus apiStatus,
        GnuCashWriteRoundTripPaths paths,
        GnuCashNativeSessionCounts before,
        GnuCashNativeSessionCounts after)
    {
        var isReady = before == after;
        return CreateStatus(
            apiStatus,
            paths,
            isReady
                ? "The native GnuCash engine saved and reopened the copied book."
                : "The copied book reopened, but native summary counts changed.",
            before,
            after,
            backendErrorCode: 0);
    }

    private static GnuCashNativeWriteRoundTripStatus CreateStatus(
        GnuCashNativeApiStatus? apiStatus,
        GnuCashWriteRoundTripPaths paths,
        string message,
        GnuCashNativeSessionCounts? before = null,
        GnuCashNativeSessionCounts? after = null,
        int? backendErrorCode = null,
        string? backendErrorMessage = null)
    {
        var isReady = apiStatus?.IsReady == true &&
                      apiStatus.CanCallFromCurrentProcess &&
                      before is not null &&
                      before == after &&
                      (backendErrorCode is null or 0);

        return new GnuCashNativeWriteRoundTripStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: apiStatus?.CanCallFromCurrentProcess ?? false,
            InstallPath: apiStatus?.InstallPath,
            DisplayVersion: apiStatus?.DisplayVersion,
            SourceBookPath: paths.SourceBookPath,
            WorkingBookPath: paths.WorkingBookPath,
            EnginePath: apiStatus?.EnginePath,
            ProcessArchitecture: apiStatus?.ProcessArchitecture ?? RuntimeInformation.ProcessArchitecture.ToString(),
            BeforeAccountCount: before?.AccountCount,
            AfterAccountCount: after?.AccountCount,
            BeforeCommodityCount: before?.CommodityCount,
            AfterCommodityCount: after?.CommodityCount,
            BeforeTransactionCount: before?.TransactionCount,
            AfterTransactionCount: after?.TransactionCount,
            BackendErrorCode: backendErrorCode,
            BackendErrorMessage: backendErrorMessage,
            CheckedPaths: apiStatus?.CheckedPaths ?? [],
            Message: message);
    }

    private sealed record GnuCashWriteRoundTripPaths(
        bool IsReady,
        string SourceBookPath,
        string WorkingBookPath,
        string Message)
    {
        public static GnuCashWriteRoundTripPaths Ready(string sourceBookPath, string workingBookPath) =>
            new(true, sourceBookPath, workingBookPath, string.Empty);

        public static GnuCashWriteRoundTripPaths Failed(
            string sourceBookPath,
            string workingBookPath,
            string message) =>
            new(false, sourceBookPath, workingBookPath, message);
    }

    private sealed record GnuCashNativeSessionCounts(
        int? AccountCount,
        int? CommodityCount,
        int? TransactionCount)
    {
        public static GnuCashNativeSessionCounts From(GnuCashNativeSessionHandle handle) =>
            new(handle.AccountCount, handle.CommodityCount, handle.TransactionCount);
    }
}
