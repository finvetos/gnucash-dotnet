using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Protocol.Contracts;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GnuCash.DotNet.Services;

/// <summary>
/// Entry point for applications using the GnuCash .NET SDK.
/// </summary>
public sealed class GnuCashClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GnuCashBridgeProcess bridgeProcess;
    private readonly ILogger<GnuCashClient> logger;
    private readonly GnuCashBridgeOptions options;

    public GnuCashClient(ILogger<GnuCashClient> logger, IOptions<GnuCashBridgeOptions> options)
        : this(
            logger,
            options,
            new GnuCashBridgeProcess(NullLogger<GnuCashBridgeProcess>.Instance, options))
    {
    }

    internal GnuCashClient(
        ILogger<GnuCashClient> logger,
        IOptions<GnuCashBridgeOptions> options,
        GnuCashBridgeProcess bridgeProcess)
    {
        this.logger = logger;
        this.options = options.Value;
        this.bridgeProcess = bridgeProcess;
    }

    /// <summary>
    /// Creates the first protocol request used by tests and early bridge handshakes.
    /// </summary>
    public BridgeRequest CreatePingRequest()
    {
        logger.LogDebug("Creating GnuCash bridge ping request.");

        return new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.Ping);
    }

    /// <summary>
    /// Validates that the configured or locally installed GnuCash runtime can be used by the bridge.
    /// </summary>
    public async Task<GnuCashInstallationStatus> ValidateInstallationAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Validating local GnuCash installation through the bridge.");

        var payload = JsonSerializer.Serialize(
            new LocateGnuCashRequest(options.InstallPath),
            SerializerOptions);
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.LocateGnuCash, payload);
        var response = await bridgeProcess.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.Succeeded)
        {
            throw new InvalidOperationException(
                CreateBridgeFailureMessage(
                    response,
                    "The GnuCash bridge could not validate the local installation."));
        }

        if (string.IsNullOrWhiteSpace(response.PayloadJson))
        {
            throw new InvalidOperationException(
                AppendDiagnostics(
                    "The GnuCash bridge returned no validation payload.",
                    response.DiagnosticOutput));
        }

        return JsonSerializer.Deserialize<GnuCashInstallationStatus>(
                   response.PayloadJson,
                   SerializerOptions) ??
               throw new InvalidOperationException("The GnuCash bridge returned an invalid validation payload.");
    }

    /// <summary>
    /// Opens a GnuCash book and returns a read-only SDK handle for querying it.
    /// </summary>
    public async Task<GnuCashBook> OpenBookAsync(
        string bookPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        logger.LogDebug("Opening GnuCash book {BookPath} through the bridge.", bookPath);

        var summary = await SendBridgeRequestAsync<GnuCashBookSummary>(
            BridgeRequestKind.OpenBook,
            CreateBookRequest(bookPath),
            cancellationToken).ConfigureAwait(false);

        return new GnuCashBook(this, Map(summary));
    }

    /// <summary>
    /// Returns the configured bridge executable path, if the application supplied one.
    /// </summary>
    public string? GetConfiguredBridgeExecutablePath() => options.BridgeExecutablePath;

    internal async Task<IReadOnlyList<GnuCashCommodity>> ListCommoditiesAsync(
        string bookPath,
        CancellationToken cancellationToken)
    {
        var commodities = await SendBridgeRequestAsync<IReadOnlyList<GnuCashCommodityRecord>>(
            BridgeRequestKind.ListCommodities,
            CreateBookRequest(bookPath),
            cancellationToken).ConfigureAwait(false);

        return commodities.Select(Map).ToArray();
    }

    internal async Task<IReadOnlyList<GnuCashAccount>> ListAccountsAsync(
        string bookPath,
        CancellationToken cancellationToken)
    {
        var accounts = await SendBridgeRequestAsync<IReadOnlyList<GnuCashAccountRecord>>(
            BridgeRequestKind.ListAccounts,
            CreateBookRequest(bookPath),
            cancellationToken).ConfigureAwait(false);

        return accounts.Select(Map).ToArray();
    }

    internal async Task<IReadOnlyList<GnuCashTransaction>> ListTransactionsAsync(
        string bookPath,
        CancellationToken cancellationToken)
    {
        var transactions = await SendBridgeRequestAsync<IReadOnlyList<GnuCashTransactionRecord>>(
            BridgeRequestKind.ListTransactions,
            CreateBookRequest(bookPath),
            cancellationToken).ConfigureAwait(false);

        return transactions.Select(Map).ToArray();
    }

    internal async Task<IReadOnlyList<GnuCashPrice>> ListPricesAsync(
        string bookPath,
        CancellationToken cancellationToken)
    {
        var prices = await SendBridgeRequestAsync<IReadOnlyList<GnuCashPriceRecord>>(
            BridgeRequestKind.ListPrices,
            CreateBookRequest(bookPath),
            cancellationToken).ConfigureAwait(false);

        return prices.Select(Map).ToArray();
    }

    internal async Task<IReadOnlyList<GnuCashCustomer>> ListCustomersAsync(
        string bookPath,
        CancellationToken cancellationToken)
    {
        var customers = await SendBridgeRequestAsync<IReadOnlyList<GnuCashCustomerRecord>>(
            BridgeRequestKind.ListCustomers,
            CreateBookRequest(bookPath),
            cancellationToken).ConfigureAwait(false);

        return customers.Select(Map).ToArray();
    }

    internal async Task<GnuCashCustomerCreateResult> CreateCustomerInCopiedBookAsync(
        string bookPath,
        GnuCashCustomerCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(request);

        var status = await SendBridgeRequestAsync<GnuCashNativeCustomerWriteStatus>(
            BridgeRequestKind.ValidateNativeCustomerWrite,
            new GnuCashNativeCustomerWriteRequest(
                bookPath,
                request.CustomerId,
                request.CustomerName,
                request.CurrencySpace,
                request.CurrencyId,
                request.WorkingBookPath,
                options.InstallPath),
            cancellationToken).ConfigureAwait(false);

        return Map(status);
    }

    internal async Task<GnuCashTransactionCreateResult> CreateTransactionInCopiedBookAsync(
        string bookPath,
        GnuCashTransactionCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(request);

        var status = await SendBridgeRequestAsync<GnuCashNativeTransactionWriteStatus>(
            BridgeRequestKind.ValidateNativeTransactionWrite,
            new GnuCashNativeTransactionWriteRequest(
                bookPath,
                request.Description,
                request.PostedAt,
                request.Splits.Select(Map).ToArray(),
                request.CurrencySpace,
                request.CurrencyId,
                request.Number,
                request.WorkingBookPath,
                options.InstallPath),
            cancellationToken).ConfigureAwait(false);

        return Map(status);
    }

    internal async Task<GnuCashTransactionBatchCreateResult> CreateTransactionsInCopiedBookAsync(
        string bookPath,
        GnuCashTransactionBatchCreateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(request);

        var status = await SendBridgeRequestAsync<GnuCashNativeTransactionBatchWriteStatus>(
            BridgeRequestKind.ValidateNativeTransactionBatchWrite,
            new GnuCashNativeTransactionBatchWriteRequest(
                bookPath,
                request.Transactions.Select(MapTransactionItem).ToArray(),
                request.WorkingBookPath,
                options.InstallPath),
            cancellationToken).ConfigureAwait(false);

        return Map(status);
    }

    private GnuCashBookRequest CreateBookRequest(string bookPath) =>
        new(bookPath, Map(options.ReadMode), options.InstallPath);

    private async Task<TPayload> SendBridgeRequestAsync<TPayload>(
        BridgeRequestKind kind,
        object payload,
        CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        var request = new BridgeRequest(Guid.NewGuid(), kind, payloadJson);
        var response = await bridgeProcess.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.Succeeded)
        {
            throw new InvalidOperationException(
                CreateBridgeFailureMessage(response, $"The GnuCash bridge could not process {kind}."));
        }

        if (string.IsNullOrWhiteSpace(response.PayloadJson))
        {
            throw new InvalidOperationException(
                AppendDiagnostics(
                    $"The GnuCash bridge returned no payload for {kind}.",
                    response.DiagnosticOutput));
        }

        return JsonSerializer.Deserialize<TPayload>(
                   response.PayloadJson,
                   SerializerOptions) ??
               throw new InvalidOperationException(
                   AppendDiagnostics(
                       $"The GnuCash bridge returned an invalid payload for {kind}.",
                       response.DiagnosticOutput));
    }

    private static string CreateBridgeFailureMessage(BridgeResponse response, string fallbackMessage) =>
        AppendDiagnostics(
            response.ErrorMessage ?? fallbackMessage,
            response.DiagnosticOutput);

    private static string AppendDiagnostics(string message, string? diagnostics) =>
        string.IsNullOrWhiteSpace(diagnostics)
            ? message
            : message + Environment.NewLine + "Bridge diagnostics:" + Environment.NewLine + diagnostics;

    private static GnuCashBookInfo Map(GnuCashBookSummary summary) =>
        new(
            summary.BookPath,
            summary.FileFormat,
            summary.BookId,
            summary.CommodityCount,
            summary.AccountCount,
            summary.TransactionCount,
            summary.SplitCount,
            summary.PriceCount);

    private static GnuCashCommodity Map(GnuCashCommodityRecord commodity) =>
        new(
            commodity.Space,
            commodity.Id,
            commodity.Name,
            commodity.XCode,
            commodity.Fraction);

    private static GnuCashAccount Map(GnuCashAccountRecord account) =>
        new(
            account.Id,
            account.Name,
            account.Type,
            account.ParentId,
            account.CommoditySpace,
            account.CommodityId,
            account.Code,
            account.Description,
            account.IsPlaceholder);

    private static GnuCashTransaction Map(GnuCashTransactionRecord transaction) =>
        new(
            transaction.Id,
            transaction.Number,
            transaction.Description,
            transaction.CurrencySpace,
            transaction.CurrencyId,
            transaction.PostedAt,
            transaction.EnteredAt,
            transaction.Splits.Select(Map).ToArray());

    private static GnuCashSplit Map(GnuCashSplitRecord split) =>
        new(
            split.Id,
            split.AccountId,
            split.Memo,
            split.Action,
            split.ReconciledState,
            Map(split.Value),
            Map(split.Quantity),
            split.ReconciledAt);

    private static GnuCashAmount Map(GnuCashAmountRecord amount) =>
        new(amount.RawValue, amount.Numerator, amount.Denominator);

    private static GnuCashAmountRecord Map(GnuCashAmount amount) =>
        new(amount.RawValue, amount.Numerator, amount.Denominator);

    private static GnuCashPrice Map(GnuCashPriceRecord price) =>
        new(
            price.Id,
            price.CommoditySpace,
            price.CommodityId,
            price.CurrencySpace,
            price.CurrencyId,
            price.Time,
            price.Source,
            price.Type,
            Map(price.Value));

    private static GnuCashCustomer Map(GnuCashCustomerRecord customer) =>
        new(
            customer.Id,
            customer.CustomerId,
            customer.Name,
            customer.CurrencySpace,
            customer.CurrencyId);

    private static GnuCashCustomerCreateResult Map(GnuCashNativeCustomerWriteStatus status) =>
        new(
            status.IsReady,
            status.SourceBookPath,
            status.WorkingBookPath,
            status.CustomerId,
            status.CustomerName,
            status.CurrencySpace,
            status.CurrencyId,
            status.CreatedCustomerGuid,
            status.BeforeCustomerCount,
            status.AfterCustomerCount,
            status.FoundAfterReopen,
            status.BackendErrorCode,
            status.BackendErrorMessage,
            status.Message);

    private static GnuCashTransactionCreateResult Map(GnuCashNativeTransactionWriteStatus status) =>
        new(
            status.IsReady,
            status.SourceBookPath,
            status.WorkingBookPath,
            status.Description,
            status.CurrencySpace,
            status.CurrencyId,
            status.Number,
            status.CreatedTransactionGuid,
            status.SplitCount,
            status.BeforeTransactionCount,
            status.AfterTransactionCount,
            status.FoundAfterReopen,
            status.BackendErrorCode,
            status.BackendErrorMessage,
            status.Message);

    private static GnuCashTransactionBatchCreateResult Map(GnuCashNativeTransactionBatchWriteStatus status) =>
        new(
            status.IsReady,
            status.SourceBookPath,
            status.WorkingBookPath,
            status.RequestedTransactionCount,
            status.CreatedTransactionCount,
            status.SplitCount,
            status.CreatedTransactions.Select(Map).ToArray(),
            status.BeforeTransactionCount,
            status.AfterTransactionCount,
            status.FoundAfterReopenCount,
            status.BackendErrorCode,
            status.BackendErrorMessage,
            status.Message);

    private static GnuCashCreatedTransactionResult Map(GnuCashNativeCreatedTransactionRecord transaction) =>
        new(
            transaction.Index,
            transaction.Description,
            transaction.Number,
            transaction.CreatedTransactionGuid,
            transaction.SplitCount,
            transaction.FoundAfterReopen);

    private static GnuCashNativeTransactionSplitWriteRequest Map(GnuCashTransactionSplitCreateRequest split) =>
        new(
            split.AccountId,
            Map(split.Value),
            split.Quantity is null ? null : Map(split.Quantity),
            split.Memo,
            split.Action);

    private static GnuCashNativeTransactionWriteItemRequest MapTransactionItem(GnuCashTransactionCreateRequest transaction) =>
        new(
            transaction.Description,
            transaction.PostedAt,
            transaction.Splits.Select(Map).ToArray(),
            transaction.CurrencySpace,
            transaction.CurrencyId,
            transaction.Number);

    private static GnuCashBookReadBackend Map(GnuCashBookReadMode readMode) =>
        readMode switch
        {
            GnuCashBookReadMode.Native => GnuCashBookReadBackend.Native,
            GnuCashBookReadMode.NativeThenXml => GnuCashBookReadBackend.NativeThenXml,
            _ => GnuCashBookReadBackend.Xml
        };
}
