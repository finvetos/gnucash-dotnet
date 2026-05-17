using System.Runtime.InteropServices;

namespace GnuCash.DotNet.Bridge.Native;

internal static class GnuCashNativeMethods
{
    private const string EngineLibrary = "libgnc-engine.dll";
    private const string GnuCashModuleLibrary = "libgnc-module.dll";
    private const string CoreUtilsLibrary = "libgnc-core-utils.dll";
    private const string GlibLibrary = "libglib-2.0-0.dll";

    [DllImport(CoreUtilsLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_gbr_init(IntPtr error);

    [DllImport(CoreUtilsLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gnc_environment_setup();

    [DllImport(GnuCashModuleLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gnc_module_system_init();

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gnc_engine_init(int argc, IntPtr argv);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gnc_engine_shutdown();

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_uri_normalize_uri(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string uri,
        int allowPassword);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_book_new();

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_session_new(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qof_session_destroy(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qof_session_begin(
        IntPtr session,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string uri,
        SessionOpenMode mode);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qof_session_load(IntPtr session, IntPtr percentageFunc);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qof_session_save(IntPtr session, IntPtr percentageFunc);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qof_session_end(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_session_get_book(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qof_session_get_error(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_session_get_error_message(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_session_get_file_path(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_session_get_url(IntPtr session);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_book_get_root_account(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_book_count_transactions(IntPtr book);

    [DllImport(GlibLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_free(IntPtr value);

    internal static string? PtrToUtf8String(IntPtr value) =>
        value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(value);
}

internal enum SessionOpenMode
{
    NormalOpen = 0,
    NewStore = 2,
    NewOverwrite = 3,
    ReadOnly = 4,
    BreakLock = 5
}
