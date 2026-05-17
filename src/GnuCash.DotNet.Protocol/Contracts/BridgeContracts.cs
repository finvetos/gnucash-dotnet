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
    Ping = 0,
    LocateGnuCash = 1,
    OpenBook = 2,
    ListAccounts = 3,
    Shutdown = 4,
    ListCommodities = 5,
    ListTransactions = 6,
    ListPrices = 7,
    ValidateNativeApi = 8,
    ValidateNativeSession = 9
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

/// <summary>
/// Optional payload for locating a GnuCash installation.
/// </summary>
public sealed record LocateGnuCashRequest(string? InstallPath = null);

/// <summary>
/// Request payload for validating a native GnuCash session against a local book.
/// </summary>
public sealed record GnuCashNativeSessionRequest(string BookPath, string? InstallPath = null);

/// <summary>
/// Result of validating the local GnuCash installation needed by the bridge.
/// </summary>
public sealed record GnuCashInstallationStatus(
    bool IsReady,
    string? InstallPath,
    string? DisplayVersion,
    string? Source,
    IReadOnlyList<string> MissingPaths,
    IReadOnlyList<string> CheckedPaths,
    string Message);

/// <summary>
/// Result of validating the native GnuCash engine API surface needed by write-capable bridge features.
/// </summary>
public sealed record GnuCashNativeApiStatus(
    bool IsReady,
    bool CanCallFromCurrentProcess,
    string? InstallPath,
    string? DisplayVersion,
    string? EnginePath,
    string ProcessArchitecture,
    IReadOnlyList<string> RequiredExports,
    IReadOnlyList<string> MissingExports,
    IReadOnlyList<string> CheckedPaths,
    string Message);

/// <summary>
/// Result of opening a book through the installed native GnuCash runtime.
/// </summary>
public sealed record GnuCashNativeSessionStatus(
    bool IsReady,
    bool CanCallFromCurrentProcess,
    string? InstallPath,
    string? DisplayVersion,
    string BookPath,
    string? EnginePath,
    string? SessionFilePath,
    string? SessionUrl,
    string ProcessArchitecture,
    bool HasBook,
    bool HasRootAccount,
    int? TransactionCount,
    int? BackendErrorCode,
    string? BackendErrorMessage,
    IReadOnlyList<string> CheckedPaths,
    string Message);

/// <summary>
/// Request payload for operations that read a GnuCash book file.
/// </summary>
public sealed record GnuCashBookRequest(string BookPath);

/// <summary>
/// Summary returned when the bridge can open and inspect a book.
/// </summary>
public sealed record GnuCashBookSummary(
    string BookPath,
    string FileFormat,
    string? BookId,
    int CommodityCount,
    int AccountCount,
    int TransactionCount,
    int SplitCount,
    int PriceCount);

/// <summary>
/// Commodity or currency definition read from a GnuCash book.
/// </summary>
public sealed record GnuCashCommodityRecord(
    string Space,
    string Id,
    string? Name,
    string? XCode,
    int Fraction);

/// <summary>
/// Account definition read from a GnuCash book.
/// </summary>
public sealed record GnuCashAccountRecord(
    string Id,
    string Name,
    string Type,
    string? ParentId,
    string? CommoditySpace,
    string? CommodityId,
    string? Code,
    string? Description,
    bool IsPlaceholder);

/// <summary>
/// Rational amount as represented by GnuCash, such as 12345/100.
/// </summary>
public sealed record GnuCashAmountRecord(
    string RawValue,
    long? Numerator,
    long? Denominator);

/// <summary>
/// Split line inside a transaction.
/// </summary>
public sealed record GnuCashSplitRecord(
    string Id,
    string AccountId,
    string? Memo,
    string? Action,
    string ReconciledState,
    GnuCashAmountRecord Value,
    GnuCashAmountRecord Quantity,
    DateTimeOffset? ReconciledAt);

/// <summary>
/// Transaction and its split lines.
/// </summary>
public sealed record GnuCashTransactionRecord(
    string Id,
    string? Number,
    string? Description,
    string? CurrencySpace,
    string? CurrencyId,
    DateTimeOffset? PostedAt,
    DateTimeOffset? EnteredAt,
    IReadOnlyList<GnuCashSplitRecord> Splits);

/// <summary>
/// Price database entry read from a GnuCash book.
/// </summary>
public sealed record GnuCashPriceRecord(
    string Id,
    string CommoditySpace,
    string CommodityId,
    string CurrencySpace,
    string CurrencyId,
    DateTimeOffset? Time,
    string? Source,
    string? Type,
    GnuCashAmountRecord Value);
