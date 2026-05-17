using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Reads customer business objects through the installed native GnuCash engine.
/// </summary>
public sealed class GnuCashNativeCustomerReader
{
    private const string CustomerEntityType = "gncCustomer";

    private readonly GnuCashNativeRuntime runtime;

    public GnuCashNativeCustomerReader()
        : this(new GnuCashNativeRuntime())
    {
    }

    internal GnuCashNativeCustomerReader(GnuCashNativeRuntime runtime)
    {
        this.runtime = runtime;
    }

    public GnuCashNativeCustomerReadResult Read(
        string bookPath,
        string? explicitInstallPath = null)
    {
        var pathRead = NormalizeBookPath(bookPath);
        if (!pathRead.IsReady)
        {
            return GnuCashNativeCustomerReadResult.Failed(pathRead.BookPath, pathRead.Message);
        }

        var runtimeState = runtime.Prepare(explicitInstallPath);
        if (!runtimeState.IsPrepared)
        {
            return GnuCashNativeCustomerReadResult.Failed(pathRead.BookPath, runtimeState.ApiStatus.Message);
        }

        return ReadPrepared(pathRead.BookPath);
    }

    private static GnuCashNativeCustomerReadResult ReadPrepared(string normalizedBookPath)
    {
        try
        {
            using var handle = GnuCashNativeSessionHandle.OpenReadOnly(normalizedBookPath);
            var customers = ReadCustomers(handle.Book);
            return GnuCashNativeCustomerReadResult.Ready(normalizedBookPath, customers);
        }
        catch (GnuCashNativeOperationException ex)
        {
            return GnuCashNativeCustomerReadResult.Failed(
                normalizedBookPath,
                ex.BackendErrorMessage ?? ex.Message);
        }
        catch (Exception ex) when (
            ex is DllNotFoundException or
                  EntryPointNotFoundException or
                  BadImageFormatException or
                  SEHException or
                  InvalidOperationException)
        {
            return GnuCashNativeCustomerReadResult.Failed(
                normalizedBookPath,
                "The native GnuCash customer read failed: " + ex.Message);
        }
    }

    private static IReadOnlyList<GnuCashCustomerRecord> ReadCustomers(IntPtr book)
    {
        var collection = book == IntPtr.Zero
            ? IntPtr.Zero
            : GnuCashNativeMethods.qof_book_get_collection(book, CustomerEntityType);
        if (collection == IntPtr.Zero)
        {
            return [];
        }

        var customers = new List<GnuCashCustomerRecord>();
        QofInstanceForeachCallback callback = (customer, _) =>
        {
            if (customer != IntPtr.Zero)
            {
                customers.Add(ReadCustomer(customer));
            }
        };

        GnuCashNativeMethods.qof_collection_foreach(collection, callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return customers
            .OrderBy(customer => customer.CustomerId, StringComparer.Ordinal)
            .ToArray();
    }

    private static GnuCashCustomerRecord ReadCustomer(IntPtr customer)
    {
        var currency = GnuCashNativeMethods.gncCustomerGetCurrency(customer);
        return new GnuCashCustomerRecord(
            ReadGuid(customer) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.gncCustomerGetID(customer)) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.gncCustomerGetName(customer)) ?? string.Empty,
            ReadCommoditySpace(currency),
            ReadCommodityId(currency));
    }

    private static GnuCashNativeBookPathRead NormalizeBookPath(string bookPath)
    {
        if (string.IsNullOrWhiteSpace(bookPath))
        {
            return new GnuCashNativeBookPathRead(false, string.Empty, "A GnuCash book path is required.");
        }

        var normalizedBookPath = Path.GetFullPath(bookPath);
        return File.Exists(normalizedBookPath)
            ? new GnuCashNativeBookPathRead(true, normalizedBookPath, string.Empty)
            : new GnuCashNativeBookPathRead(false, normalizedBookPath, "The requested GnuCash book does not exist.");
    }

    private static string? ReadCommoditySpace(IntPtr commodity) =>
        commodity == IntPtr.Zero
            ? null
            : ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_namespace(commodity));

    private static string? ReadCommodityId(IntPtr commodity) =>
        commodity == IntPtr.Zero
            ? null
            : ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_mnemonic(commodity));

    private static string? ReadGuid(IntPtr instance)
    {
        var guid = GnuCashNativeMethods.qof_instance_get_guid(instance);
        if (guid == IntPtr.Zero)
        {
            return null;
        }

        var text = GnuCashNativeMethods.guid_to_string(guid);
        try
        {
            return GnuCashNativeMethods.PtrToUtf8String(text);
        }
        finally
        {
            if (text != IntPtr.Zero)
            {
                GnuCashNativeMethods.g_free(text);
            }
        }
    }

    private static string? ReadBorrowedString(IntPtr value) =>
        GnuCashNativeMethods.PtrToUtf8String(value);
}

public sealed record GnuCashNativeCustomerReadResult(
    string BookPath,
    IReadOnlyList<GnuCashCustomerRecord> Customers,
    bool IsReady,
    string Message)
{
    public static GnuCashNativeCustomerReadResult Ready(
        string bookPath,
        IReadOnlyList<GnuCashCustomerRecord> customers) =>
        new(bookPath, customers, true, "The native GnuCash customer read completed.");

    public static GnuCashNativeCustomerReadResult Failed(string bookPath, string message) =>
        new(bookPath, [], false, message);
}
