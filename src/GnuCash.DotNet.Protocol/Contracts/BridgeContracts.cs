namespace GnuCash.DotNet.Protocol.Contracts;

/// <summary>
/// Versioned bridge protocol constants shared by the SDK and bridge process.
/// </summary>
public static class BridgeProtocol
{
    public const int CurrentVersion = 1;
}

/// <summary>
/// High-level operations that the SDK can ask the bridge to perform.
/// </summary>
public enum BridgeRequestKind
{
    Ping,
    LocateGnuCash,
    OpenBook,
    ListAccounts,
    Shutdown
}

/// <summary>
/// Request envelope passed from the AnyCPU SDK to the win-x86 bridge.
/// </summary>
public sealed record BridgeRequest(
    Guid Id,
    BridgeRequestKind Kind,
    string? PayloadJson = null,
    int ProtocolVersion = BridgeProtocol.CurrentVersion);

/// <summary>
/// Response envelope returned by the bridge.
/// </summary>
public sealed record BridgeResponse(
    Guid Id,
    bool Succeeded,
    string? PayloadJson = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    int ProtocolVersion = BridgeProtocol.CurrentVersion);

/// <summary>
/// Minimal health information used by early process-start and compatibility checks.
/// </summary>
public sealed record BridgeHandshake(
    int ProtocolVersion,
    string BridgeVersion,
    string ProcessArchitecture,
    string? GnuCashInstallPath);