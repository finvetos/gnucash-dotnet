using System.Runtime.InteropServices;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

public sealed partial class GnuCashNativeTransactionWriteValidator
{
    public GnuCashNativeTransactionBatchWriteStatus ValidateBatch(GnuCashNativeTransactionBatchWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = ValidateBatchInput(request);
        if (!input.IsReady)
        {
            return CreateBatchStatus(null, input, input.Message);
        }

        var runtimeState = runtime.Prepare(request.InstallPath);
        if (!runtimeState.IsPrepared)
        {
            return CreateBatchStatus(runtimeState.ApiStatus, input, runtimeState.ApiStatus.Message);
        }

        return ValidateBatchPrepared(runtimeState.ApiStatus, input);
    }

    private static GnuCashNativeTransactionBatchWriteStatus ValidateBatchPrepared(
        GnuCashNativeApiStatus apiStatus,
        GnuCashTransactionBatchWriteInput input)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(input.WorkingBookPath)!);
            File.Copy(input.SourceBookPath, input.WorkingBookPath, overwrite: false);

            using var writable = GnuCashNativeSessionHandle.OpenWritable(input.WorkingBookPath);
            var beforeCount = writable.TransactionCount;
            var created = new List<GnuCashNativeCreatedTransactionRecord>();
            for (var index = 0; index < input.Transactions.Count; index++)
            {
                var transaction = input.Transactions[index];
                var createdGuid = CreateTransaction(writable.Book, writable.RootAccount, transaction);
                created.Add(new GnuCashNativeCreatedTransactionRecord(
                    index,
                    transaction.Description,
                    transaction.Number,
                    createdGuid,
                    transaction.Splits.Count,
                    false));
            }

            writable.Save();

            using var reopened = GnuCashNativeSessionHandle.OpenReadOnly(input.WorkingBookPath);
            var afterCount = reopened.TransactionCount;
            var verified = created
                .Select(transaction => transaction with
                {
                    FoundAfterReopen = FindTransactionById(
                        reopened.RootAccount,
                        transaction.CreatedTransactionGuid ?? string.Empty) != IntPtr.Zero
                })
                .ToArray();

            return CreateBatchLoadedStatus(apiStatus, input, verified, beforeCount, afterCount);
        }
        catch (GnuCashNativeOperationException ex)
        {
            return CreateBatchStatus(
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
            return CreateBatchStatus(apiStatus, input, "The native transaction batch write failed: " + ex.Message);
        }
    }

    private static GnuCashTransactionBatchWriteInput ValidateBatchInput(GnuCashNativeTransactionBatchWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceBookPath))
        {
            return new GnuCashTransactionBatchWriteInput(false, string.Empty, string.Empty, [], "A source book path is required.");
        }

        if (request.Transactions is null || request.Transactions.Count == 0)
        {
            return new GnuCashTransactionBatchWriteInput(false, string.Empty, string.Empty, [], "At least one transaction is required.");
        }

        var source = Path.GetFullPath(request.SourceBookPath);
        if (!File.Exists(source))
        {
            return new GnuCashTransactionBatchWriteInput(false, source, string.Empty, [], "The source GnuCash book does not exist.");
        }

        var working = string.IsNullOrWhiteSpace(request.WorkingBookPath)
            ? CreateDefaultBatchWorkingPath(source)
            : Path.GetFullPath(request.WorkingBookPath);
        if (File.Exists(working))
        {
            return new GnuCashTransactionBatchWriteInput(false, source, working, [], "The working book path already exists.");
        }

        var transactions = new List<GnuCashTransactionWriteInput>();
        for (var index = 0; index < request.Transactions.Count; index++)
        {
            var transaction = request.Transactions[index];
            var validationMessage = ValidateTransactionShape(transaction);
            if (validationMessage is not null)
            {
                return new GnuCashTransactionBatchWriteInput(
                    false,
                    source,
                    working,
                    [],
                    $"Transaction {index + 1}: {validationMessage}");
            }

            var splitRead = ReadSplits(transaction.Splits);
            if (splitRead.FailureMessage is not null)
            {
                return new GnuCashTransactionBatchWriteInput(
                    false,
                    source,
                    working,
                    [],
                    $"Transaction {index + 1}: {splitRead.FailureMessage}");
            }

            if (!IsBalanced(splitRead.Splits!))
            {
                return new GnuCashTransactionBatchWriteInput(
                    false,
                    source,
                    working,
                    [],
                    $"Transaction {index + 1}: The transaction splits must balance to zero.");
            }

            transactions.Add(new GnuCashTransactionWriteInput(
                true,
                source,
                working,
                transaction.Description,
                transaction.PostedAt,
                transaction.CurrencySpace,
                transaction.CurrencyId,
                transaction.Number,
                splitRead.Splits!,
                string.Empty));
        }

        return new GnuCashTransactionBatchWriteInput(true, source, working, transactions, string.Empty);
    }

    private static string? ValidateTransactionShape(GnuCashNativeTransactionWriteItemRequest transaction)
    {
        if (string.IsNullOrWhiteSpace(transaction.Description))
        {
            return "A transaction description is required.";
        }

        if (string.IsNullOrWhiteSpace(transaction.CurrencySpace))
        {
            return "A transaction currency space is required.";
        }

        if (string.IsNullOrWhiteSpace(transaction.CurrencyId))
        {
            return "A transaction currency id is required.";
        }

        return transaction.Splits is null || transaction.Splits.Count < 2
            ? "A transaction must contain at least two splits."
            : null;
    }

    private static string CreateDefaultBatchWorkingPath(string sourceBookPath)
    {
        var extension = Path.GetExtension(sourceBookPath);
        var fileName = "transaction-batch-" + Guid.NewGuid().ToString("N") + extension;
        return Path.Combine(Path.GetTempPath(), "gnucash-dotnet-transaction-write", fileName);
    }

    private static GnuCashNativeTransactionBatchWriteStatus CreateBatchLoadedStatus(
        GnuCashNativeApiStatus apiStatus,
        GnuCashTransactionBatchWriteInput input,
        IReadOnlyList<GnuCashNativeCreatedTransactionRecord> createdTransactions,
        int? beforeCount,
        int? afterCount)
    {
        var countReady = beforeCount is not null && afterCount == beforeCount + input.Transactions.Count;
        var allFound = createdTransactions.Count == input.Transactions.Count &&
                       createdTransactions.All(transaction => transaction.FoundAfterReopen);
        return CreateBatchStatus(
            apiStatus,
            input,
            countReady && allFound
                ? "The native GnuCash engine created the transaction batch and verified it after reopen."
                : "The copied book reopened, but transaction batch verification failed.",
            createdTransactions,
            beforeCount,
            afterCount,
            backendErrorCode: 0);
    }

    private static GnuCashNativeTransactionBatchWriteStatus CreateBatchStatus(
        GnuCashNativeApiStatus? apiStatus,
        GnuCashTransactionBatchWriteInput input,
        string message,
        IReadOnlyList<GnuCashNativeCreatedTransactionRecord>? createdTransactions = null,
        int? beforeCount = null,
        int? afterCount = null,
        int? backendErrorCode = null,
        string? backendErrorMessage = null)
    {
        createdTransactions ??= [];
        var foundAfterReopen = createdTransactions.Count(transaction => transaction.FoundAfterReopen);
        var countReady = beforeCount is not null && afterCount == beforeCount + input.Transactions.Count;
        var isReady = apiStatus?.IsReady == true &&
                      apiStatus.CanCallFromCurrentProcess &&
                      createdTransactions.Count == input.Transactions.Count &&
                      foundAfterReopen == input.Transactions.Count &&
                      countReady &&
                      (backendErrorCode is null or 0);

        return new GnuCashNativeTransactionBatchWriteStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: apiStatus?.CanCallFromCurrentProcess ?? false,
            InstallPath: apiStatus?.InstallPath,
            DisplayVersion: apiStatus?.DisplayVersion,
            SourceBookPath: input.SourceBookPath,
            WorkingBookPath: input.WorkingBookPath,
            EnginePath: apiStatus?.EnginePath,
            ProcessArchitecture: apiStatus?.ProcessArchitecture ?? RuntimeInformation.ProcessArchitecture.ToString(),
            RequestedTransactionCount: input.Transactions.Count,
            CreatedTransactionCount: createdTransactions.Count,
            SplitCount: input.Transactions.Sum(transaction => transaction.Splits.Count),
            CreatedTransactions: createdTransactions,
            BeforeTransactionCount: beforeCount,
            AfterTransactionCount: afterCount,
            FoundAfterReopenCount: foundAfterReopen,
            BackendErrorCode: backendErrorCode,
            BackendErrorMessage: backendErrorMessage,
            CheckedPaths: apiStatus?.CheckedPaths ?? [],
            Message: message);
    }

    private sealed record GnuCashTransactionBatchWriteInput(
        bool IsReady,
        string SourceBookPath,
        string WorkingBookPath,
        IReadOnlyList<GnuCashTransactionWriteInput> Transactions,
        string Message);
}
