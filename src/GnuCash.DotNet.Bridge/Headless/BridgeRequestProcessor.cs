using System.Runtime.InteropServices;
using System.Text.Json;
using GnuCash.DotNet.Bridge.Discovery;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Headless;

/// <summary>
/// Handles protocol requests sent by the SDK-facing headless bridge session.
/// </summary>
public sealed class BridgeRequestProcessor
{
    private readonly GnuCashInstallationLocator locator;

    public BridgeRequestProcessor()
        : this(new GnuCashInstallationLocator())
    {
    }

    public BridgeRequestProcessor(GnuCashInstallationLocator locator)
    {
        this.locator = locator;
    }

    public BridgeResponse Process(BridgeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Kind switch
        {
            BridgeRequestKind.Ping => Succeeded(request, CreateHandshakePayload()),
            BridgeRequestKind.LocateGnuCash => Succeeded(request, CreateLocateGnuCashPayload(request)),
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
        var payload = string.IsNullOrWhiteSpace(request.PayloadJson)
            ? new LocateGnuCashRequest()
            : JsonSerializer.Deserialize<LocateGnuCashRequest>(
                  request.PayloadJson,
                  BridgeJson.SerializerOptions) ?? new LocateGnuCashRequest();

        return JsonSerializer.Serialize(
            locator.Validate(payload.InstallPath),
            BridgeJson.SerializerOptions);
    }
}
