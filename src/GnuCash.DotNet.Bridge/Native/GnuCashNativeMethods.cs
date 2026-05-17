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
    internal static extern IntPtr qof_book_get_collection(
        IntPtr book,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string entityType);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_book_count_transactions(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int qof_collection_count(IntPtr collection);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qof_collection_foreach(
        IntPtr collection,
        QofInstanceForeachCallback callback,
        IntPtr userData);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_account_n_descendants(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int xaccAccountTreeForEachTransaction(
        IntPtr account,
        GncTransactionCallback callback,
        IntPtr userData);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_account_n_children(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_account_nth_child(IntPtr account, int index);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_account_get_parent(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr qof_instance_get_guid(IntPtr instance);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr guid_to_string(IntPtr guid);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccAccountGetName(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccAccountGetCode(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccAccountGetDescription(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int xaccAccountGetType(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccAccountTypeEnumAsString(int accountType);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccAccountGetCommodity(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int xaccAccountGetPlaceholder(IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccTransGetNum(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccTransGetDescription(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccTransGetCurrency(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long xaccTransGetDate(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long xaccTransGetDateEntered(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int xaccTransCountSplits(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccTransGetSplit(IntPtr transaction, int index);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccSplitGetAccount(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccSplitGetMemo(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccSplitGetAction(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern byte xaccSplitGetReconcile(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long xaccSplitGetDateReconciled(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern GncNumeric xaccSplitGetValue(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern GncNumeric xaccSplitGetAmount(IntPtr split);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_commodity_table_get_table(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_commodity_table_get_size(IntPtr table);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_commodity_table_foreach_commodity(
        IntPtr table,
        GncCommodityTableForeachCallback callback,
        IntPtr userData);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_commodity_get_namespace(IntPtr commodity);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_commodity_get_mnemonic(IntPtr commodity);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_commodity_get_fullname(IntPtr commodity);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_commodity_get_cusip(IntPtr commodity);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_commodity_get_fraction(IntPtr commodity);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_commodity_table_lookup(
        IntPtr table,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string space,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string mnemonic);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_pricedb_get_db(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int gnc_pricedb_foreach_price(
        IntPtr priceDb,
        GncPriceForeachCallback callback,
        IntPtr userData,
        int stableOrder);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_price_get_commodity(IntPtr price);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_price_get_currency(IntPtr price);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern long gnc_price_get_time64(IntPtr price);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_price_get_source_string(IntPtr price);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gnc_price_get_typestr(IntPtr price);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern GncNumeric gnc_price_get_value(IntPtr price);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccMallocTransaction(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccTransBeginEdit(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccTransCommitEdit(IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccTransSetCurrency(IntPtr transaction, IntPtr currency);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccTransSetDescription(
        IntPtr transaction,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string description);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccTransSetNum(
        IntPtr transaction,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string number);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccTransSetDatePostedSecsNormalized(IntPtr transaction, long seconds);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr xaccMallocSplit(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccSplitSetAccount(IntPtr split, IntPtr account);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccSplitSetParent(IntPtr split, IntPtr transaction);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccSplitSetValue(IntPtr split, GncNumeric value);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccSplitSetAmount(IntPtr split, GncNumeric amount);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccSplitSetMemo(
        IntPtr split,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string memo);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void xaccSplitSetAction(
        IntPtr split,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string action);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gncCustomerCreate(IntPtr book);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gncCustomerSetID(
        IntPtr customer,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string id);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gncCustomerSetName(
        IntPtr customer,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void gncCustomerSetCurrency(IntPtr customer, IntPtr currency);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gncCustomerGetID(IntPtr customer);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gncCustomerGetName(IntPtr customer);

    [DllImport(EngineLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr gncCustomerGetCurrency(IntPtr customer);

    [DllImport(GlibLibrary, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void g_free(IntPtr value);

    internal static string? PtrToUtf8String(IntPtr value) =>
        value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(value);
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int GncCommodityTableForeachCallback(IntPtr commodity, IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int GncTransactionCallback(IntPtr transaction, IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int GncPriceForeachCallback(IntPtr price, IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void QofInstanceForeachCallback(IntPtr instance, IntPtr userData);

[StructLayout(LayoutKind.Sequential)]
internal readonly struct GncNumeric
{
    public GncNumeric(long numerator, long denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public readonly long Numerator;

    public readonly long Denominator;
}

internal enum SessionOpenMode
{
    NormalOpen = 0,
    NewStore = 2,
    NewOverwrite = 3,
    ReadOnly = 4,
    BreakLock = 5
}
