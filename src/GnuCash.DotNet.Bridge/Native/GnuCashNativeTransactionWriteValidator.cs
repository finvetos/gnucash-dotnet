using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Creates a balanced transaction in a copied book and verifies it after reopen.
/// </summary>
public sealed partial class GnuCashNativeTransactionWriteValidator
{
    private readonly GnuCashNativeRuntime runtime;

    public GnuCashNativeTransactionWriteValidator()
        : this(new GnuCashNativeRuntime())
    {
    }

    internal GnuCashNativeTransactionWriteValidator(GnuCashNativeRuntime runtime)
    {
        this.runtime = runtime;
    }

    public GnuCashNativeTransactionWriteStatus Validate(GnuCashNativeTransactionWriteRequest request)
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

    private static GnuCashNativeTransactionWriteStatus ValidatePrepared(
        GnuCashNativeApiStatus apiStatus,
        GnuCashTransactionWriteInput input)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(input.WorkingBookPath)!);
            File.Copy(input.SourceBookPath, input.WorkingBookPath, overwrite: false);

            using var writable = GnuCashNativeSessionHandle.OpenWritable(input.WorkingBookPath);
            var beforeCount = writable.TransactionCount;
            var createdGuid = CreateTransaction(writable.Book, writable.RootAccount, input);
            writable.Save();

            using var reopened = GnuCashNativeSessionHandle.OpenReadOnly(input.WorkingBookPath);
            var afterCount = reopened.TransactionCount;
            var found = FindTransactionById(reopened.RootAccount, createdGuid) != IntPtr.Zero;
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
            return CreateStatus(apiStatus, input, "The native transaction write failed: " + ex.Message);
        }
    }

    private static string CreateTransaction(
        IntPtr book,
        IntPtr rootAccount,
        GnuCashTransactionWriteInput input)
    {
        var currency = LookupCommodity(book, input.CurrencySpace, input.CurrencyId);
        if (currency == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"The requested transaction currency was not found: {input.CurrencySpace}::{input.CurrencyId}");
        }

        var transaction = GnuCashNativeMethods.xaccMallocTransaction(book);
        if (transaction == IntPtr.Zero)
        {
            throw new InvalidOperationException("The native GnuCash engine could not create a transaction.");
        }

        GnuCashNativeMethods.xaccTransBeginEdit(transaction);
        GnuCashNativeMethods.xaccTransSetCurrency(transaction, currency);
        GnuCashNativeMethods.xaccTransSetDescription(transaction, input.Description);
        if (!string.IsNullOrWhiteSpace(input.Number))
        {
            GnuCashNativeMethods.xaccTransSetNum(transaction, input.Number);
        }

        GnuCashNativeMethods.xaccTransSetDatePostedSecsNormalized(transaction, input.PostedAt.ToUnixTimeSeconds());

        foreach (var split in input.Splits)
        {
            var account = FindAccountById(rootAccount, split.AccountId);
            if (account == IntPtr.Zero)
            {
                throw new InvalidOperationException($"The requested split account was not found: {split.AccountId}");
            }

            var nativeSplit = GnuCashNativeMethods.xaccMallocSplit(book);
            if (nativeSplit == IntPtr.Zero)
            {
                throw new InvalidOperationException("The native GnuCash engine could not create a split.");
            }

            GnuCashNativeMethods.xaccSplitSetAccount(nativeSplit, account);
            GnuCashNativeMethods.xaccSplitSetParent(nativeSplit, transaction);
            GnuCashNativeMethods.xaccSplitSetValue(nativeSplit, split.Value);
            GnuCashNativeMethods.xaccSplitSetAmount(nativeSplit, split.Quantity);
            if (!string.IsNullOrWhiteSpace(split.Memo))
            {
                GnuCashNativeMethods.xaccSplitSetMemo(nativeSplit, split.Memo);
            }

            if (!string.IsNullOrWhiteSpace(split.Action))
            {
                GnuCashNativeMethods.xaccSplitSetAction(nativeSplit, split.Action);
            }
        }

        GnuCashNativeMethods.xaccTransCommitEdit(transaction);
        return ReadGuid(transaction) ?? string.Empty;
    }

    private static IntPtr LookupCommodity(IntPtr book, string space, string id)
    {
        var table = GnuCashNativeMethods.gnc_commodity_table_get_table(book);
        return table == IntPtr.Zero
            ? IntPtr.Zero
            : GnuCashNativeMethods.gnc_commodity_table_lookup(table, space, id);
    }

    private static IntPtr FindAccountById(IntPtr account, string accountId)
    {
        if (account == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        if (string.Equals(ReadGuid(account), accountId, StringComparison.Ordinal))
        {
            return account;
        }

        var childCount = GnuCashNativeMethods.gnc_account_n_children(account);
        for (var index = 0; index < childCount; index++)
        {
            var found = FindAccountById(GnuCashNativeMethods.gnc_account_nth_child(account, index), accountId);
            if (found != IntPtr.Zero)
            {
                return found;
            }
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindTransactionById(IntPtr rootAccount, string transactionId)
    {
        if (rootAccount == IntPtr.Zero || string.IsNullOrWhiteSpace(transactionId))
        {
            return IntPtr.Zero;
        }

        var found = IntPtr.Zero;
        GncTransactionCallback callback = (transaction, _) =>
        {
            if (found == IntPtr.Zero &&
                string.Equals(ReadGuid(transaction), transactionId, StringComparison.Ordinal))
            {
                found = transaction;
                return 1;
            }

            return 0;
        };

        _ = GnuCashNativeMethods.xaccAccountTreeForEachTransaction(rootAccount, callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return found;
    }

    private static GnuCashTransactionWriteInput ValidateInput(GnuCashNativeTransactionWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceBookPath))
        {
            return GnuCashTransactionWriteInput.Failed(request, string.Empty, string.Empty, [], "A source book path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return GnuCashTransactionWriteInput.Failed(request, string.Empty, string.Empty, [], "A transaction description is required.");
        }

        if (request.Splits is null || request.Splits.Count < 2)
        {
            return GnuCashTransactionWriteInput.Failed(request, string.Empty, string.Empty, [], "A transaction must contain at least two splits.");
        }

        var source = Path.GetFullPath(request.SourceBookPath);
        if (!File.Exists(source))
        {
            return GnuCashTransactionWriteInput.Failed(request, source, string.Empty, [], "The source GnuCash book does not exist.");
        }

        var splitRead = ReadSplits(request);
        if (splitRead.FailureMessage is not null)
        {
            return GnuCashTransactionWriteInput.Failed(request, source, string.Empty, [], splitRead.FailureMessage);
        }

        if (!IsBalanced(splitRead.Splits!))
        {
            return GnuCashTransactionWriteInput.Failed(request, source, string.Empty, [], "The transaction splits must balance to zero.");
        }

        var working = string.IsNullOrWhiteSpace(request.WorkingBookPath)
            ? CreateDefaultWorkingPath(source)
            : Path.GetFullPath(request.WorkingBookPath);
        return File.Exists(working)
            ? GnuCashTransactionWriteInput.Failed(request, source, working, [], "The working book path already exists.")
            : GnuCashTransactionWriteInput.Ready(request, source, working, splitRead.Splits!);
    }

    private static GnuCashSplitRead ReadSplits(GnuCashNativeTransactionWriteRequest request) =>
        ReadSplits(request.Splits);

    private static GnuCashSplitRead ReadSplits(IReadOnlyList<GnuCashNativeTransactionSplitWriteRequest> splitRequests)
    {
        var splits = new List<GnuCashTransactionSplitWriteInput>();
        foreach (var split in splitRequests)
        {
            if (string.IsNullOrWhiteSpace(split.AccountId))
            {
                return new GnuCashSplitRead(null, "Every split must include an account id.");
            }

            if (!TryReadNumeric(split.Value, out var value))
            {
                return new GnuCashSplitRead(null, $"Split value '{split.Value.RawValue}' is not a rational GnuCash amount.");
            }

            var quantity = value;
            if (split.Quantity is not null && !TryReadNumeric(split.Quantity, out quantity))
            {
                return new GnuCashSplitRead(null, $"Split quantity '{split.Quantity.RawValue}' is not a rational GnuCash amount.");
            }

            splits.Add(new GnuCashTransactionSplitWriteInput(
                split.AccountId,
                value,
                quantity,
                split.Memo,
                split.Action));
        }

        return new GnuCashSplitRead(splits, null);
    }

    private static bool TryReadNumeric(GnuCashAmountRecord amount, out GncNumeric numeric)
    {
        if (amount.Numerator is not null && amount.Denominator is not null and not 0)
        {
            numeric = new GncNumeric(amount.Numerator.Value, amount.Denominator.Value);
            return true;
        }

        numeric = default;
        return false;
    }

    private static bool IsBalanced(IReadOnlyList<GnuCashTransactionSplitWriteInput> splits)
    {
        var numerator = 0L;
        var denominator = 1L;
        foreach (var split in splits)
        {
            (numerator, denominator) = Add(
                numerator,
                denominator,
                split.Value.Numerator,
                split.Value.Denominator);
        }

        return numerator == 0;
    }

    private static (long Numerator, long Denominator) Add(
        long leftNumerator,
        long leftDenominator,
        long rightNumerator,
        long rightDenominator)
    {
        var denominator = checked(leftDenominator / GreatestCommonDivisor(leftDenominator, rightDenominator) * rightDenominator);
        var numerator = checked(
            leftNumerator * (denominator / leftDenominator) +
            rightNumerator * (denominator / rightDenominator));

        return (numerator, denominator);
    }

    private static long GreatestCommonDivisor(long left, long right)
    {
        while (right != 0)
        {
            var remainder = left % right;
            left = right;
            right = remainder;
        }

        return left == 0 ? 1 : Math.Abs(left);
    }

    private static string CreateDefaultWorkingPath(string sourceBookPath)
    {
        var extension = Path.GetExtension(sourceBookPath);
        var fileName = "transaction-" + Guid.NewGuid().ToString("N") + extension;
        return Path.Combine(Path.GetTempPath(), "gnucash-dotnet-transaction-write", fileName);
    }

    private static GnuCashNativeTransactionWriteStatus CreateLoadedStatus(
        GnuCashNativeApiStatus apiStatus,
        GnuCashTransactionWriteInput input,
        string createdGuid,
        int? beforeCount,
        int? afterCount,
        bool found)
    {
        var isReady = found && beforeCount is not null && afterCount == beforeCount + 1;
        return CreateStatus(
            apiStatus,
            input,
            isReady
                ? "The native GnuCash engine created a transaction and verified it after reopen."
                : "The copied book reopened, but the transaction verification failed.",
            createdGuid,
            beforeCount,
            afterCount,
            found,
            backendErrorCode: 0);
    }

    private static GnuCashNativeTransactionWriteStatus CreateStatus(
        GnuCashNativeApiStatus? apiStatus,
        GnuCashTransactionWriteInput input,
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

        return new GnuCashNativeTransactionWriteStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: apiStatus?.CanCallFromCurrentProcess ?? false,
            InstallPath: apiStatus?.InstallPath,
            DisplayVersion: apiStatus?.DisplayVersion,
            SourceBookPath: input.SourceBookPath,
            WorkingBookPath: input.WorkingBookPath,
            EnginePath: apiStatus?.EnginePath,
            ProcessArchitecture: apiStatus?.ProcessArchitecture ?? RuntimeInformation.ProcessArchitecture.ToString(),
            Description: input.Description,
            CurrencySpace: input.CurrencySpace,
            CurrencyId: input.CurrencyId,
            Number: input.Number,
            CreatedTransactionGuid: createdGuid,
            SplitCount: input.Splits.Count,
            BeforeTransactionCount: beforeCount,
            AfterTransactionCount: afterCount,
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

    private sealed record GnuCashTransactionWriteInput(
        bool IsReady,
        string SourceBookPath,
        string WorkingBookPath,
        string Description,
        DateTimeOffset PostedAt,
        string CurrencySpace,
        string CurrencyId,
        string? Number,
        IReadOnlyList<GnuCashTransactionSplitWriteInput> Splits,
        string Message)
    {
        public static GnuCashTransactionWriteInput Ready(
            GnuCashNativeTransactionWriteRequest request,
            string sourceBookPath,
            string workingBookPath,
            IReadOnlyList<GnuCashTransactionSplitWriteInput> splits) =>
            Create(true, request, sourceBookPath, workingBookPath, splits, string.Empty);

        public static GnuCashTransactionWriteInput Failed(
            GnuCashNativeTransactionWriteRequest request,
            string sourceBookPath,
            string workingBookPath,
            IReadOnlyList<GnuCashTransactionSplitWriteInput> splits,
            string message) =>
            Create(false, request, sourceBookPath, workingBookPath, splits, message);

        private static GnuCashTransactionWriteInput Create(
            bool isReady,
            GnuCashNativeTransactionWriteRequest request,
            string sourceBookPath,
            string workingBookPath,
            IReadOnlyList<GnuCashTransactionSplitWriteInput> splits,
            string message) =>
            new(
                isReady,
                sourceBookPath,
                workingBookPath,
                request.Description,
                request.PostedAt,
                request.CurrencySpace,
                request.CurrencyId,
                request.Number,
                splits,
                message);
    }

    private sealed record GnuCashTransactionSplitWriteInput(
        string AccountId,
        GncNumeric Value,
        GncNumeric Quantity,
        string? Memo,
        string? Action);

    private sealed record GnuCashSplitRead(
        IReadOnlyList<GnuCashTransactionSplitWriteInput>? Splits,
        string? FailureMessage);
}
