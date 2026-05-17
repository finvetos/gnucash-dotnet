namespace GnuCash.DotNet.Bridge.Native;

internal sealed class GnuCashNativeSessionHandle : IDisposable
{
    private IntPtr session;

    private GnuCashNativeSessionHandle(
        string bookPath,
        IntPtr session,
        IntPtr book,
        IntPtr rootAccount)
    {
        BookPath = bookPath;
        this.session = session;
        Book = book;
        RootAccount = rootAccount;
        SessionFilePath = GnuCashNativeMethods.PtrToUtf8String(
            GnuCashNativeMethods.qof_session_get_file_path(session));
        SessionUrl = GnuCashNativeMethods.PtrToUtf8String(
            GnuCashNativeMethods.qof_session_get_url(session));
        TransactionCount = CountTransactions(book);
        AccountCount = CountAccounts(rootAccount);
        CommodityCount = CountCommodities(book);
    }

    public string BookPath { get; }

    public IntPtr Book { get; }

    public IntPtr RootAccount { get; }

    public string? SessionFilePath { get; }

    public string? SessionUrl { get; }

    public int? TransactionCount { get; }

    public int? AccountCount { get; }

    public int? CommodityCount { get; }

    public static GnuCashNativeSessionHandle OpenReadOnly(string bookPath) =>
        Open(bookPath, SessionOpenMode.ReadOnly);

    public static GnuCashNativeSessionHandle OpenWritable(string bookPath) =>
        Open(bookPath, SessionOpenMode.NormalOpen);

    public void Save()
    {
        if (session == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(GnuCashNativeSessionHandle));
        }

        GnuCashNativeMethods.qof_session_save(session, IntPtr.Zero);
        ThrowIfBackendError(session, "The native GnuCash engine could not save the book.");
    }

    private static GnuCashNativeSessionHandle Open(string bookPath, SessionOpenMode mode)
    {
        var session = IntPtr.Zero;
        var bookUri = IntPtr.Zero;

        try
        {
            var book = GnuCashNativeMethods.qof_book_new();
            session = GnuCashNativeMethods.qof_session_new(book);
            if (session == IntPtr.Zero)
            {
                throw new GnuCashNativeOperationException(
                    "The native GnuCash engine could not create a session.");
            }

            bookUri = GnuCashNativeMethods.gnc_uri_normalize_uri(bookPath, allowPassword: 0);
            var sessionUri = GnuCashNativeMethods.PtrToUtf8String(bookUri) ?? bookPath;

            GnuCashNativeMethods.qof_session_begin(session, sessionUri, mode);
            ThrowIfBackendError(session, "The native GnuCash engine could not begin a session.");

            GnuCashNativeMethods.qof_session_load(session, IntPtr.Zero);
            ThrowIfBackendError(session, "The native GnuCash engine could not load the book.");

            var loadedBook = GnuCashNativeMethods.qof_session_get_book(session);
            var rootAccount = loadedBook == IntPtr.Zero
                ? IntPtr.Zero
                : GnuCashNativeMethods.gnc_book_get_root_account(loadedBook);

            return new GnuCashNativeSessionHandle(bookPath, session, loadedBook, rootAccount);
        }
        catch
        {
            if (session != IntPtr.Zero)
            {
                CloseSession(session);
            }

            throw;
        }
        finally
        {
            if (bookUri != IntPtr.Zero)
            {
                GnuCashNativeMethods.g_free(bookUri);
            }
        }
    }

    public void Dispose()
    {
        if (session == IntPtr.Zero)
        {
            return;
        }

        CloseSession(session);
        session = IntPtr.Zero;
    }

    private static int? CountTransactions(IntPtr book) =>
        book == IntPtr.Zero ? null : GnuCashNativeMethods.gnc_book_count_transactions(book);

    private static int? CountAccounts(IntPtr rootAccount) =>
        rootAccount == IntPtr.Zero
            ? null
            : 1 + GnuCashNativeMethods.gnc_account_n_descendants(rootAccount);

    private static int? CountCommodities(IntPtr book)
    {
        if (book == IntPtr.Zero)
        {
            return null;
        }

        var table = GnuCashNativeMethods.gnc_commodity_table_get_table(book);
        return table == IntPtr.Zero ? null : GnuCashNativeMethods.gnc_commodity_table_get_size(table);
    }

    private static void ThrowIfBackendError(IntPtr session, string message)
    {
        var backendError = ReadBackendError(session);
        if (backendError.Code == 0)
        {
            return;
        }

        throw new GnuCashNativeOperationException(
            message,
            backendError.Code,
            backendError.Message);
    }

    private static (int Code, string? Message) ReadBackendError(IntPtr session)
    {
        var code = GnuCashNativeMethods.qof_session_get_error(session);
        var message = GnuCashNativeMethods.PtrToUtf8String(
            GnuCashNativeMethods.qof_session_get_error_message(session));

        return (code, string.IsNullOrWhiteSpace(message) ? null : message);
    }

    private static void CloseSession(IntPtr session)
    {
        GnuCashNativeMethods.qof_session_end(session);
        GnuCashNativeMethods.qof_session_destroy(session);
    }
}
