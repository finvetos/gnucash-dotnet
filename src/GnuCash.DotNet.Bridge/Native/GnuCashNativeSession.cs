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
        var session = IntPtr.Zero;
        var bookUri = IntPtr.Zero;

        try
        {
            var book = GnuCashNativeMethods.qof_book_new();
            session = GnuCashNativeMethods.qof_session_new(book);

            if (session == IntPtr.Zero)
            {
                return CreateStatus(
                    runtimeState.ApiStatus,
                    normalizedBookPath,
                    message: "The native GnuCash engine could not create a session.");
            }

            bookUri = GnuCashNativeMethods.gnc_uri_normalize_uri(normalizedBookPath, allowPassword: 0);
            var sessionUri = GnuCashNativeMethods.PtrToUtf8String(bookUri) ?? normalizedBookPath;

            GnuCashNativeMethods.qof_session_begin(session, sessionUri, SessionOpenMode.ReadOnly);
            var beginError = ReadBackendError(session);
            if (beginError.Code != 0)
            {
                return CreateStatus(
                    runtimeState.ApiStatus,
                    normalizedBookPath,
                    backendErrorCode: beginError.Code,
                    backendErrorMessage: beginError.Message,
                    message: "The native GnuCash engine could not begin a read-only session.");
            }

            GnuCashNativeMethods.qof_session_load(session, IntPtr.Zero);
            var loadError = ReadBackendError(session);
            var loadedBook = GnuCashNativeMethods.qof_session_get_book(session);
            var rootAccount = loadedBook == IntPtr.Zero
                ? IntPtr.Zero
                : GnuCashNativeMethods.gnc_book_get_root_account(loadedBook);
            int? transactionCount = loadedBook == IntPtr.Zero
                ? null
                : GnuCashNativeMethods.gnc_book_count_transactions(loadedBook);

            return CreateLoadedStatus(
                runtimeState.ApiStatus,
                normalizedBookPath,
                session,
                loadedBook,
                rootAccount,
                transactionCount,
                loadError);
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
        finally
        {
            if (bookUri != IntPtr.Zero)
            {
                GnuCashNativeMethods.g_free(bookUri);
            }

            if (session != IntPtr.Zero)
            {
                CloseSession(session);
            }
        }
    }

    private static GnuCashNativeSessionStatus CreateLoadedStatus(
        GnuCashNativeApiStatus apiStatus,
        string normalizedBookPath,
        IntPtr session,
        IntPtr loadedBook,
        IntPtr rootAccount,
        int? transactionCount,
        (int Code, string? Message) loadError) =>
        CreateStatus(
            apiStatus,
            normalizedBookPath,
            sessionFilePath: GnuCashNativeMethods.PtrToUtf8String(
                GnuCashNativeMethods.qof_session_get_file_path(session)),
            sessionUrl: GnuCashNativeMethods.PtrToUtf8String(
                GnuCashNativeMethods.qof_session_get_url(session)),
            hasBook: loadedBook != IntPtr.Zero,
            hasRootAccount: rootAccount != IntPtr.Zero,
            transactionCount: transactionCount,
            backendErrorCode: loadError.Code,
            backendErrorMessage: loadError.Message,
            message: loadError.Code == 0
                ? "The native GnuCash engine opened the book read-only."
                : "The native GnuCash engine could not load the book.");

    private static void CloseSession(IntPtr session)
    {
        GnuCashNativeMethods.qof_session_end(session);
        GnuCashNativeMethods.qof_session_destroy(session);
    }

    private static (int Code, string? Message) ReadBackendError(IntPtr session)
    {
        var code = GnuCashNativeMethods.qof_session_get_error(session);
        var message = GnuCashNativeMethods.PtrToUtf8String(
            GnuCashNativeMethods.qof_session_get_error_message(session));

        return (code, string.IsNullOrWhiteSpace(message) ? null : message);
    }

    private static GnuCashNativeSessionStatus CreateStatus(
        GnuCashNativeApiStatus? apiStatus,
        string bookPath,
        string? sessionFilePath = null,
        string? sessionUrl = null,
        bool hasBook = false,
        bool hasRootAccount = false,
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
            TransactionCount: transactionCount,
            BackendErrorCode: backendErrorCode,
            BackendErrorMessage: backendErrorMessage,
            CheckedPaths: apiStatus?.CheckedPaths ?? [],
            Message: message ?? "The native GnuCash session is not ready.");
    }
}
