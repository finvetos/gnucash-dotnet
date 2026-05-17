using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Creates a customer in a copied book and verifies the business object after reopen.
/// </summary>
public sealed class GnuCashNativeCustomerWriteValidator
{
    private const string CustomerEntityType = "gncCustomer";

    private readonly GnuCashNativeRuntime runtime;

    public GnuCashNativeCustomerWriteValidator()
        : this(new GnuCashNativeRuntime())
    {
    }

    internal GnuCashNativeCustomerWriteValidator(GnuCashNativeRuntime runtime)
    {
        this.runtime = runtime;
    }

    public GnuCashNativeCustomerWriteStatus Validate(GnuCashNativeCustomerWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = ValidateInput(request);
        if (!input.IsReady)
        {
            return CreateStatus(null, input, input.Message);
        }

        var runtimeState = runtime.Prepare(request.InstallPath);
        if (!runtimeState.IsPrepared)
        {
            return CreateStatus(runtimeState.ApiStatus, input, runtimeState.ApiStatus.Message);
        }

        return ValidatePrepared(runtimeState.ApiStatus, input);
    }

    private static GnuCashNativeCustomerWriteStatus ValidatePrepared(
        GnuCashNativeApiStatus apiStatus,
        GnuCashCustomerWriteInput input)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(input.WorkingBookPath)!);
            File.Copy(input.SourceBookPath, input.WorkingBookPath, overwrite: false);

            using var writable = GnuCashNativeSessionHandle.OpenWritable(input.WorkingBookPath);
            var beforeCount = CountCustomers(writable.Book);
            var createdGuid = CreateCustomer(writable.Book, input);
            writable.Save();

            using var reopened = GnuCashNativeSessionHandle.OpenReadOnly(input.WorkingBookPath);
            var afterCount = CountCustomers(reopened.Book);
            var found = FindCustomer(reopened.Book, input.CustomerId, input.CustomerName) is not null;
            return CreateLoadedStatus(apiStatus, input, createdGuid, beforeCount, afterCount, found);
        }
        catch (GnuCashNativeOperationException ex)
        {
            return CreateStatus(
                apiStatus,
                input,
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
            return CreateStatus(apiStatus, input, "The native customer write failed: " + ex.Message);
        }
    }

    private static string CreateCustomer(IntPtr book, GnuCashCustomerWriteInput input)
    {
        var currency = LookupCommodity(book, input.CurrencySpace, input.CurrencyId);
        if (currency == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"The requested customer currency was not found: {input.CurrencySpace}::{input.CurrencyId}");
        }

        var customer = GnuCashNativeMethods.gncCustomerCreate(book);
        if (customer == IntPtr.Zero)
        {
            throw new InvalidOperationException("The native GnuCash engine could not create a customer.");
        }

        GnuCashNativeMethods.gncCustomerSetID(customer, input.CustomerId);
        GnuCashNativeMethods.gncCustomerSetName(customer, input.CustomerName);
        GnuCashNativeMethods.gncCustomerSetCurrency(customer, currency);
        return ReadGuid(customer) ?? string.Empty;
    }

    private static IntPtr LookupCommodity(IntPtr book, string space, string id)
    {
        var table = GnuCashNativeMethods.gnc_commodity_table_get_table(book);
        return table == IntPtr.Zero
            ? IntPtr.Zero
            : GnuCashNativeMethods.gnc_commodity_table_lookup(table, space, id);
    }

    private static int CountCustomers(IntPtr book)
    {
        var collection = GetCustomerCollection(book);
        return collection == IntPtr.Zero ? 0 : GnuCashNativeMethods.qof_collection_count(collection);
    }

    private static GnuCashNativeCustomerRecord? FindCustomer(
        IntPtr book,
        string customerId,
        string customerName)
    {
        var collection = GetCustomerCollection(book);
        if (collection == IntPtr.Zero)
        {
            return null;
        }

        GnuCashNativeCustomerRecord? found = null;
        QofInstanceForeachCallback callback = (instance, _) =>
        {
            if (found is null)
            {
                var customer = ReadCustomer(instance);
                if (string.Equals(customer.Id, customerId, StringComparison.Ordinal) &&
                    string.Equals(customer.Name, customerName, StringComparison.Ordinal))
                {
                    found = customer;
                }
            }
        };

        GnuCashNativeMethods.qof_collection_foreach(collection, callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return found;
    }

    private static IntPtr GetCustomerCollection(IntPtr book) =>
        book == IntPtr.Zero
            ? IntPtr.Zero
            : GnuCashNativeMethods.qof_book_get_collection(book, CustomerEntityType);

    private static GnuCashNativeCustomerRecord ReadCustomer(IntPtr customer)
    {
        var currency = GnuCashNativeMethods.gncCustomerGetCurrency(customer);
        return new GnuCashNativeCustomerRecord(
            ReadGuid(customer) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.gncCustomerGetID(customer)) ?? string.Empty,
            ReadBorrowedString(GnuCashNativeMethods.gncCustomerGetName(customer)) ?? string.Empty,
            ReadCommoditySpace(currency),
            ReadCommodityId(currency));
    }

    private static GnuCashCustomerWriteInput ValidateInput(GnuCashNativeCustomerWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceBookPath))
        {
            return GnuCashCustomerWriteInput.Failed(request, string.Empty, string.Empty, "A source book path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerId) || string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return GnuCashCustomerWriteInput.Failed(request, string.Empty, string.Empty, "A customer id and name are required.");
        }

        var source = Path.GetFullPath(request.SourceBookPath);
        if (!File.Exists(source))
        {
            return GnuCashCustomerWriteInput.Failed(request, source, string.Empty, "The source GnuCash book does not exist.");
        }

        var working = string.IsNullOrWhiteSpace(request.WorkingBookPath)
            ? CreateDefaultWorkingPath(source)
            : Path.GetFullPath(request.WorkingBookPath);
        return File.Exists(working)
            ? GnuCashCustomerWriteInput.Failed(request, source, working, "The working book path already exists.")
            : GnuCashCustomerWriteInput.Ready(request, source, working);
    }

    private static string CreateDefaultWorkingPath(string sourceBookPath)
    {
        var extension = Path.GetExtension(sourceBookPath);
        var fileName = "customer-" + Guid.NewGuid().ToString("N") + extension;
        return Path.Combine(Path.GetTempPath(), "gnucash-dotnet-customer-write", fileName);
    }

    private static GnuCashNativeCustomerWriteStatus CreateLoadedStatus(
        GnuCashNativeApiStatus apiStatus,
        GnuCashCustomerWriteInput input,
        string createdGuid,
        int beforeCount,
        int afterCount,
        bool found)
    {
        var isReady = found && afterCount == beforeCount + 1;
        return CreateStatus(
            apiStatus,
            input,
            isReady
                ? "The native GnuCash engine created a customer and verified it after reopen."
                : "The copied book reopened, but the customer verification failed.",
            createdGuid,
            beforeCount,
            afterCount,
            found,
            backendErrorCode: 0);
    }

    private static GnuCashNativeCustomerWriteStatus CreateStatus(
        GnuCashNativeApiStatus? apiStatus,
        GnuCashCustomerWriteInput input,
        string message,
        string? createdGuid = null,
        int? beforeCount = null,
        int? afterCount = null,
        bool found = false,
        int? backendErrorCode = null,
        string? backendErrorMessage = null)
    {
        var countReady = beforeCount is not null && afterCount == beforeCount + 1;
        var isReady = apiStatus?.IsReady == true &&
                      apiStatus.CanCallFromCurrentProcess &&
                      found &&
                      countReady &&
                      (backendErrorCode is null or 0);

        return new GnuCashNativeCustomerWriteStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: apiStatus?.CanCallFromCurrentProcess ?? false,
            InstallPath: apiStatus?.InstallPath,
            DisplayVersion: apiStatus?.DisplayVersion,
            SourceBookPath: input.SourceBookPath,
            WorkingBookPath: input.WorkingBookPath,
            EnginePath: apiStatus?.EnginePath,
            ProcessArchitecture: apiStatus?.ProcessArchitecture ?? RuntimeInformation.ProcessArchitecture.ToString(),
            CustomerId: input.CustomerId,
            CustomerName: input.CustomerName,
            CurrencySpace: input.CurrencySpace,
            CurrencyId: input.CurrencyId,
            CreatedCustomerGuid: createdGuid,
            BeforeCustomerCount: beforeCount,
            AfterCustomerCount: afterCount,
            FoundAfterReopen: found,
            BackendErrorCode: backendErrorCode,
            BackendErrorMessage: backendErrorMessage,
            CheckedPaths: apiStatus?.CheckedPaths ?? [],
            Message: message);
    }

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

    private static string? ReadCommoditySpace(IntPtr commodity) =>
        commodity == IntPtr.Zero
            ? null
            : ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_namespace(commodity));

    private static string? ReadCommodityId(IntPtr commodity) =>
        commodity == IntPtr.Zero
            ? null
            : ReadBorrowedString(GnuCashNativeMethods.gnc_commodity_get_mnemonic(commodity));

    private static string? ReadBorrowedString(IntPtr value) =>
        GnuCashNativeMethods.PtrToUtf8String(value);

    private sealed record GnuCashCustomerWriteInput(
        bool IsReady,
        string SourceBookPath,
        string WorkingBookPath,
        string CustomerId,
        string CustomerName,
        string CurrencySpace,
        string CurrencyId,
        string Message)
    {
        public static GnuCashCustomerWriteInput Ready(
            GnuCashNativeCustomerWriteRequest request,
            string sourceBookPath,
            string workingBookPath) =>
            Create(true, request, sourceBookPath, workingBookPath, string.Empty);

        public static GnuCashCustomerWriteInput Failed(
            GnuCashNativeCustomerWriteRequest request,
            string sourceBookPath,
            string workingBookPath,
            string message) =>
            Create(false, request, sourceBookPath, workingBookPath, message);

        private static GnuCashCustomerWriteInput Create(
            bool isReady,
            GnuCashNativeCustomerWriteRequest request,
            string sourceBookPath,
            string workingBookPath,
            string message) =>
            new(
                isReady,
                sourceBookPath,
                workingBookPath,
                request.CustomerId,
                request.CustomerName,
                request.CurrencySpace,
                request.CurrencyId,
                message);
    }

    private sealed record GnuCashNativeCustomerRecord(
        string Guid,
        string Id,
        string Name,
        string? CurrencySpace,
        string? CurrencyId);
}
