using GnuCash.DotNet.Options;
using GnuCash.DotNet.Protocol.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GnuCash.DotNet.Services;

/// <summary>
/// Entry point for applications using the GnuCash .NET SDK.
/// </summary>
public sealed class GnuCashClient
{
    private readonly ILogger<GnuCashClient> logger;
    private readonly GnuCashBridgeOptions options;

    public GnuCashClient(ILogger<GnuCashClient> logger, IOptions<GnuCashBridgeOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
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
    /// Returns the configured bridge executable path, if the application supplied one.
    /// </summary>
    public string? GetConfiguredBridgeExecutablePath() => options.BridgeExecutablePath;
}