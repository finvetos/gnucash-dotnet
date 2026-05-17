using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Opens GnuCash books through the installed native engine from the win-x86 bridge.
/// </summary>
public sealed class GnuCashNativeSession
{
    private readonly GnuCashNativeRuntime runtime;

    public GnuCashNativeSession()
        : this(new GnuCashNativeRuntime())
    {
    }

    internal GnuCashNativeSession(GnuCashNativeRuntime runtime)
    {
        this.runtime = runtime;
    }

    public GnuCashNativeSessionStatus ValidateReadOnlyOpen(
        string bookPath,
        string? explicitInstallPath = null)
    {
        if (string.IsNullOrWhiteSpace(bookPath))
        {
            return CreateStatus(
                apiStatus: null,
                bookPath: string.Empty,
                message: "A GnuCash book path is required.");
        }

        var normalizedBookPath = Path.GetFullPath(bookPath);
        if (!File.Exists(normalizedBookPath))
        {
            return CreateStatus(
                apiStatus: null,
                bookPath: normalizedBookPath,
                message: "The requested GnuCash book does not exist.");
        }

        var runtimeState = runtime.Prepare(explicitInstallPath);
        if (!runtimeState.IsPrepared)
        {
            return CreateStatus(
                runtimeState.ApiStatus,
                normalizedBookPath,
                message: runtimeState.ApiStatus.Message);
        }

        return ValidatePreparedReadOnlyOpen(runtimeState, normalizedBookPath);
    }

    private static GnuCashNativeSessionStatus ValidatePreparedReadOnlyOpen(
        GnuCashNativeRuntimeState runtimeState,
        string normalizedBookPath)
    {
        try
        {
            using var handle = GnuCashNativeSessionHandle.OpenReadOnly(normalizedBookPath);
            return CreateLoadedStatus(runtimeState.ApiStatus, normalizedBookPath, handle);
        }
        catch (GnuCashNativeOperationException ex)
        {
            return CreateStatus(
                runtimeState.ApiStatus,
                normalizedBookPath,
                backendErrorCode: ex.BackendErrorCode,
                backendErrorMessage: ex.BackendErrorMessage,
                message: ex.Message);
        }
        catch (Exception ex) when (
            ex is DllNotFoundException or
                  EntryPointNotFoundException or
                  BadImageFormatException or
                  SEHException or
                  InvalidOperationException)
        {
            return CreateStatus(
                runtimeState.ApiStatus,
                normalizedBookPath,
                message: "The native GnuCash runtime call failed: " + ex.Message);
        }
    }

    private static GnuCashNativeSessionStatus CreateLoadedStatus(
        GnuCashNativeApiStatus apiStatus,
        string normalizedBookPath,
        GnuCashNativeSessionHandle handle) =>
        CreateStatus(
            apiStatus,
            normalizedBookPath,
            sessionFilePath: handle.SessionFilePath,
            sessionUrl: handle.SessionUrl,
            hasBook: handle.Book != IntPtr.Zero,
            hasRootAccount: handle.RootAccount != IntPtr.Zero,
            accountCount: handle.AccountCount,
            commodityCount: handle.CommodityCount,
            transactionCount: handle.TransactionCount,
            backendErrorCode: 0,
            message: "The native GnuCash engine opened the book read-only.");

    private static GnuCashNativeSessionStatus CreateStatus(
        GnuCashNativeApiStatus? apiStatus,
        string bookPath,
        string? sessionFilePath = null,
        string? sessionUrl = null,
        bool hasBook = false,
        bool hasRootAccount = false,
        int? accountCount = null,
        int? commodityCount = null,
        int? transactionCount = null,
        int? backendErrorCode = null,
        string? backendErrorMessage = null,
        string? message = null)
    {
        var isReady = apiStatus?.IsReady == true &&
                      apiStatus.CanCallFromCurrentProcess &&
                      hasBook &&
                      hasRootAccount &&
                      (backendErrorCode is null or 0);

        return new GnuCashNativeSessionStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: apiStatus?.CanCallFromCurrentProcess ?? false,
            InstallPath: apiStatus?.InstallPath,
            DisplayVersion: apiStatus?.DisplayVersion,
            BookPath: bookPath,
            EnginePath: apiStatus?.EnginePath,
            SessionFilePath: sessionFilePath,
            SessionUrl: sessionUrl,
            ProcessArchitecture: apiStatus?.ProcessArchitecture ?? RuntimeInformation.ProcessArchitecture.ToString(),
            HasBook: hasBook,
            HasRootAccount: hasRootAccount,
            AccountCount: accountCount,
            CommodityCount: commodityCount,
            TransactionCount: transactionCount,
            BackendErrorCode: backendErrorCode,
            BackendErrorMessage: backendErrorMessage,
            CheckedPaths: apiStatus?.CheckedPaths ?? [],
            Message: message ?? "The native GnuCash session is not ready.");
    }
}
