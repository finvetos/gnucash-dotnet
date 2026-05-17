using System.Runtime.InteropServices;
using System.Text.Json;
using GnuCash.DotNet.Bridge.Books;
using GnuCash.DotNet.Bridge.Discovery;
using GnuCash.DotNet.Bridge.Native;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Headless;

/// <summary>
/// Handles protocol requests sent by the SDK-facing headless bridge session.
/// </summary>
public sealed class BridgeRequestProcessor
{
    private readonly GnuCashBookReader bookReader;
    private readonly GnuCashInstallationLocator locator;
    private readonly GnuCashNativeApiProbe nativeApiProbe;
    private readonly GnuCashNativeSession nativeSession;
    private readonly GnuCashNativeReadParityChecker nativeReadParityChecker;
    private readonly GnuCashNativeWriteRoundTripValidator nativeWriteRoundTripValidator;
    private readonly GnuCashNativeCustomerWriteValidator nativeCustomerWriteValidator;
    private readonly GnuCashNativeTransactionWriteValidator nativeTransactionWriteValidator;
    private readonly GnuCashNativeBookReader nativeBookReader;
    private readonly GnuCashNativeCustomerReader nativeCustomerReader;

    public BridgeRequestProcessor()
        : this(
            new GnuCashInstallationLocator(),
            new GnuCashBookReader(),
            new GnuCashNativeApiProbe(),
            new GnuCashNativeSession(),
            new GnuCashNativeReadParityChecker(),
            new GnuCashNativeWriteRoundTripValidator(),
            new GnuCashNativeCustomerWriteValidator(),
            new GnuCashNativeTransactionWriteValidator(),
            new GnuCashNativeBookReader(),
            new GnuCashNativeCustomerReader())
    {
    }

    public BridgeRequestProcessor(
        GnuCashInstallationLocator locator,
        GnuCashBookReader bookReader,
        GnuCashNativeApiProbe nativeApiProbe,
        GnuCashNativeSession nativeSession,
        GnuCashNativeReadParityChecker nativeReadParityChecker,
        GnuCashNativeWriteRoundTripValidator nativeWriteRoundTripValidator,
        GnuCashNativeCustomerWriteValidator nativeCustomerWriteValidator,
        GnuCashNativeTransactionWriteValidator nativeTransactionWriteValidator,
        GnuCashNativeBookReader nativeBookReader,
        GnuCashNativeCustomerReader nativeCustomerReader)
    {
        this.locator = locator;
        this.bookReader = bookReader;
        this.nativeApiProbe = nativeApiProbe;
        this.nativeSession = nativeSession;
        this.nativeReadParityChecker = nativeReadParityChecker;
        this.nativeWriteRoundTripValidator = nativeWriteRoundTripValidator;
        this.nativeCustomerWriteValidator = nativeCustomerWriteValidator;
        this.nativeTransactionWriteValidator = nativeTransactionWriteValidator;
        this.nativeBookReader = nativeBookReader;
        this.nativeCustomerReader = nativeCustomerReader;
    }

    public BridgeResponse Process(BridgeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Kind switch
        {
            BridgeRequestKind.Ping => Succeeded(request, CreateHandshakePayload()),
            BridgeRequestKind.LocateGnuCash => Succeeded(request, CreateLocateGnuCashPayload(request)),
            BridgeRequestKind.OpenBook => Succeeded(request, CreateOpenBookPayload(request)),
            BridgeRequestKind.ListCommodities => Succeeded(request, CreateListCommoditiesPayload(request)),
            BridgeRequestKind.ListAccounts => Succeeded(request, CreateListAccountsPayload(request)),
            BridgeRequestKind.ListTransactions => Succeeded(request, CreateListTransactionsPayload(request)),
            BridgeRequestKind.ListPrices => Succeeded(request, CreateListPricesPayload(request)),
            BridgeRequestKind.ValidateNativeApi => Succeeded(request, CreateValidateNativeApiPayload(request)),
            BridgeRequestKind.ValidateNativeSession => Succeeded(request, CreateValidateNativeSessionPayload(request)),
            BridgeRequestKind.ValidateNativeReadParity => Succeeded(request, CreateValidateNativeReadParityPayload(request)),
            BridgeRequestKind.ValidateNativeWriteRoundTrip => Succeeded(request, CreateValidateNativeWriteRoundTripPayload(request)),
            BridgeRequestKind.ValidateNativeCustomerWrite => Succeeded(request, CreateValidateNativeCustomerWritePayload(request)),
            BridgeRequestKind.ValidateNativeTransactionWrite => Succeeded(request, CreateValidateNativeTransactionWritePayload(request)),
            BridgeRequestKind.ListCustomers => Succeeded(request, CreateListCustomersPayload(request)),
            BridgeRequestKind.Shutdown => Succeeded(request),
            _ => Failed(
                request,
                "UnsupportedRequest",
                $"Request kind '{request.Kind}' is not implemented by this bridge.")
        };
    }

    public static BridgeHandshake CreateHandshake() =>
        new(
            BridgeProtocol.CurrentVersion,
            typeof(BridgeRequestProcessor).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.GetEnvironmentVariable("GNUCASH_HOME"));

    private static BridgeResponse Succeeded(BridgeRequest request, string? payloadJson = null) =>
        new(request.Id, true, payloadJson);

    private static BridgeResponse Failed(BridgeRequest request, string errorCode, string errorMessage) =>
        new(request.Id, false, ErrorCode: errorCode, ErrorMessage: errorMessage);

    private static string CreateHandshakePayload() =>
        JsonSerializer.Serialize(CreateHandshake(), BridgeJson.SerializerOptions);

    private string CreateLocateGnuCashPayload(BridgeRequest request)
    {
        return JsonSerializer.Serialize(
            locator.Validate(DeserializeLocateRequest(request).InstallPath),
            BridgeJson.SerializerOptions);
    }

    private string CreateValidateNativeApiPayload(BridgeRequest request) =>
        JsonSerializer.Serialize(
            nativeApiProbe.Validate(DeserializeLocateRequest(request).InstallPath),
            BridgeJson.SerializerOptions);

    private string CreateValidateNativeSessionPayload(BridgeRequest request)
    {
        var payload = DeserializeNativeSessionRequest(request);
        return JsonSerializer.Serialize(
            nativeSession.ValidateReadOnlyOpen(payload.BookPath, payload.InstallPath),
            BridgeJson.SerializerOptions);
    }

    private string CreateValidateNativeReadParityPayload(BridgeRequest request)
    {
        var payload = DeserializeNativeSessionRequest(request);
        return JsonSerializer.Serialize(
            nativeReadParityChecker.Validate(payload.BookPath, payload.InstallPath),
            BridgeJson.SerializerOptions);
    }

    private string CreateValidateNativeWriteRoundTripPayload(BridgeRequest request)
    {
        var payload = DeserializeWriteRoundTripRequest(request);
        return JsonSerializer.Serialize(
            nativeWriteRoundTripValidator.Validate(
                payload.SourceBookPath,
                payload.WorkingBookPath,
                payload.InstallPath),
            BridgeJson.SerializerOptions);
    }

    private string CreateValidateNativeCustomerWritePayload(BridgeRequest request) =>
        JsonSerializer.Serialize(
            nativeCustomerWriteValidator.Validate(DeserializeCustomerWriteRequest(request)),
            BridgeJson.SerializerOptions);

    private string CreateValidateNativeTransactionWritePayload(BridgeRequest request) =>
        JsonSerializer.Serialize(
            nativeTransactionWriteValidator.Validate(DeserializeTransactionWriteRequest(request)),
            BridgeJson.SerializerOptions);

    private string CreateOpenBookPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        var snapshot = ReadBookSnapshot(payload);
        return JsonSerializer.Serialize(
            new GnuCashBookSummary(
                snapshot.BookPath,
                snapshot.FileFormat,
                snapshot.BookId,
                snapshot.Commodities.Count,
                snapshot.Accounts.Count,
                snapshot.Transactions.Count,
                snapshot.Transactions.Sum(transaction => transaction.Splits.Count),
                snapshot.Prices.Count),
            BridgeJson.SerializerOptions);
    }

    private string CreateListCommoditiesPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(ReadBookSnapshot(payload).Commodities, BridgeJson.SerializerOptions);
    }

    private string CreateListAccountsPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(ReadBookSnapshot(payload).Accounts, BridgeJson.SerializerOptions);
    }

    private string CreateListTransactionsPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(ReadBookSnapshot(payload).Transactions, BridgeJson.SerializerOptions);
    }

    private string CreateListPricesPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(ReadBookSnapshot(payload).Prices, BridgeJson.SerializerOptions);
    }

    private string CreateListCustomersPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        var customers = nativeCustomerReader.Read(payload.BookPath, payload.InstallPath);
        if (!customers.IsReady)
        {
            throw new InvalidOperationException(customers.Message);
        }

        return JsonSerializer.Serialize(customers.Customers, BridgeJson.SerializerOptions);
    }

    private BridgeBookSnapshot ReadBookSnapshot(GnuCashBookRequest request)
    {
        if (request.ReadBackend is GnuCashBookReadBackend.Native or GnuCashBookReadBackend.NativeThenXml)
        {
            var nativeRead = nativeBookReader.ReadCoreBookData(request.BookPath, request.InstallPath);
            if (nativeRead.IsReady)
            {
                return new BridgeBookSnapshot(
                    nativeRead.BookPath,
                    "GnuCashNative",
                    nativeRead.BookId,
                    nativeRead.Commodities,
                    nativeRead.Accounts,
                    nativeRead.Transactions,
                    nativeRead.Prices);
            }

            if (request.ReadBackend == GnuCashBookReadBackend.Native)
            {
                throw new InvalidOperationException(nativeRead.Message);
            }
        }

        var summary = bookReader.Open(request.BookPath);
        return new BridgeBookSnapshot(
            summary.BookPath,
            summary.FileFormat,
            summary.BookId,
            bookReader.ListCommodities(request.BookPath),
            bookReader.ListAccounts(request.BookPath),
            bookReader.ListTransactions(request.BookPath),
            bookReader.ListPrices(request.BookPath));
    }

    private static GnuCashBookRequest DeserializeBookRequest(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            throw new ArgumentException("A book request payload is required.", nameof(request));
        }

        return JsonSerializer.Deserialize<GnuCashBookRequest>(
                   request.PayloadJson,
                   BridgeJson.SerializerOptions) ??
               throw new ArgumentException("A valid book request payload is required.", nameof(request));
    }

    private static LocateGnuCashRequest DeserializeLocateRequest(BridgeRequest request) =>
        string.IsNullOrWhiteSpace(request.PayloadJson)
            ? new LocateGnuCashRequest()
            : JsonSerializer.Deserialize<LocateGnuCashRequest>(
                  request.PayloadJson,
                  BridgeJson.SerializerOptions) ?? new LocateGnuCashRequest();

    private static GnuCashNativeSessionRequest DeserializeNativeSessionRequest(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            throw new ArgumentException("A native session validation request payload is required.", nameof(request));
        }

        return JsonSerializer.Deserialize<GnuCashNativeSessionRequest>(
                   request.PayloadJson,
                   BridgeJson.SerializerOptions) ??
               throw new ArgumentException("A valid native session validation request payload is required.", nameof(request));
    }

    private static GnuCashNativeWriteRoundTripRequest DeserializeWriteRoundTripRequest(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            throw new ArgumentException("A native write round-trip request payload is required.", nameof(request));
        }

        return JsonSerializer.Deserialize<GnuCashNativeWriteRoundTripRequest>(
                   request.PayloadJson,
                   BridgeJson.SerializerOptions) ??
               throw new ArgumentException("A valid native write round-trip request payload is required.", nameof(request));
    }

    private static GnuCashNativeCustomerWriteRequest DeserializeCustomerWriteRequest(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            throw new ArgumentException("A native customer write request payload is required.", nameof(request));
        }

        return JsonSerializer.Deserialize<GnuCashNativeCustomerWriteRequest>(
                   request.PayloadJson,
                   BridgeJson.SerializerOptions) ??
               throw new ArgumentException("A valid native customer write request payload is required.", nameof(request));
    }

    private static GnuCashNativeTransactionWriteRequest DeserializeTransactionWriteRequest(BridgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            throw new ArgumentException("A native transaction write request payload is required.", nameof(request));
        }

        return JsonSerializer.Deserialize<GnuCashNativeTransactionWriteRequest>(
                   request.PayloadJson,
                   BridgeJson.SerializerOptions) ??
               throw new ArgumentException("A valid native transaction write request payload is required.", nameof(request));
    }

    private sealed record BridgeBookSnapshot(
        string BookPath,
        string FileFormat,
        string? BookId,
        IReadOnlyList<GnuCashCommodityRecord> Commodities,
        IReadOnlyList<GnuCashAccountRecord> Accounts,
        IReadOnlyList<GnuCashTransactionRecord> Transactions,
        IReadOnlyList<GnuCashPriceRecord> Prices);
}
