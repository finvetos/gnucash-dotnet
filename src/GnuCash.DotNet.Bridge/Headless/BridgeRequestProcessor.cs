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

    public BridgeRequestProcessor()
        : this(
            new GnuCashInstallationLocator(),
            new GnuCashBookReader(),
            new GnuCashNativeApiProbe(),
            new GnuCashNativeSession())
    {
    }

    public BridgeRequestProcessor(
        GnuCashInstallationLocator locator,
        GnuCashBookReader bookReader,
        GnuCashNativeApiProbe nativeApiProbe,
        GnuCashNativeSession nativeSession)
    {
        this.locator = locator;
        this.bookReader = bookReader;
        this.nativeApiProbe = nativeApiProbe;
        this.nativeSession = nativeSession;
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

    private string CreateOpenBookPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(bookReader.Open(payload.BookPath), BridgeJson.SerializerOptions);
    }

    private string CreateListCommoditiesPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(bookReader.ListCommodities(payload.BookPath), BridgeJson.SerializerOptions);
    }

    private string CreateListAccountsPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(bookReader.ListAccounts(payload.BookPath), BridgeJson.SerializerOptions);
    }

    private string CreateListTransactionsPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(bookReader.ListTransactions(payload.BookPath), BridgeJson.SerializerOptions);
    }

    private string CreateListPricesPayload(BridgeRequest request)
    {
        var payload = DeserializeBookRequest(request);
        return JsonSerializer.Serialize(bookReader.ListPrices(payload.BookPath), BridgeJson.SerializerOptions);
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
}
