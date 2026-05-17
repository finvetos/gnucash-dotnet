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
                response.ErrorMessage ?? "The GnuCash bridge could not validate the local installation.");
        }

        if (string.IsNullOrWhiteSpace(response.PayloadJson))
        {
            throw new InvalidOperationException("The GnuCash bridge returned no validation payload.");
        }

        return JsonSerializer.Deserialize<GnuCashInstallationStatus>(
                   response.PayloadJson,
                   SerializerOptions) ??
               throw new InvalidOperationException("The GnuCash bridge returned an invalid validation payload.");
    }

    /// <summary>
    /// Returns the configured bridge executable path, if the application supplied one.
    /// </summary>
    public string? GetConfiguredBridgeExecutablePath() => options.BridgeExecutablePath;
}
