using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Services;

public sealed partial class GnuCashClient
{
    /// <summary>
    /// Validates the native GnuCash API exports required by bridge-backed SDK features.
    /// </summary>
    public Task<GnuCashNativeApiStatus> ValidateNativeApiAsync(
        CancellationToken cancellationToken = default) =>
        SendBridgeRequestAsync<GnuCashNativeApiStatus>(
            BridgeRequestKind.ValidateNativeApi,
            new LocateGnuCashRequest(options.InstallPath),
            cancellationToken);

    /// <summary>
    /// Inventories native exports from the configured or locally installed GnuCash runtime.
    /// </summary>
    public Task<GnuCashNativeExportInventoryStatus> InventoryNativeExportsAsync(
        CancellationToken cancellationToken = default) =>
        SendBridgeRequestAsync<GnuCashNativeExportInventoryStatus>(
            BridgeRequestKind.InventoryNativeExports,
            new LocateGnuCashRequest(options.InstallPath),
            cancellationToken);

    /// <summary>
    /// Validates that the native engine can open a book in a read-only session.
    /// </summary>
    public Task<GnuCashNativeSessionStatus> ValidateNativeSessionAsync(
        string bookPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);

        return SendBridgeRequestAsync<GnuCashNativeSessionStatus>(
            BridgeRequestKind.ValidateNativeSession,
            new GnuCashNativeSessionRequest(bookPath, options.InstallPath),
            cancellationToken);
    }

    /// <summary>
    /// Compares native engine reads with the XML reader for the same book.
    /// </summary>
    public Task<GnuCashNativeReadParityStatus> ValidateNativeReadParityAsync(
        string bookPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);

        return SendBridgeRequestAsync<GnuCashNativeReadParityStatus>(
            BridgeRequestKind.ValidateNativeReadParity,
            new GnuCashNativeSessionRequest(bookPath, options.InstallPath),
            cancellationToken);
    }

    /// <summary>
    /// Validates that the native engine can save and reopen a copied book.
    /// </summary>
    public Task<GnuCashNativeWriteRoundTripStatus> ValidateNativeWriteRoundTripAsync(
        string sourceBookPath,
        string? workingBookPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceBookPath);

        return SendBridgeRequestAsync<GnuCashNativeWriteRoundTripStatus>(
            BridgeRequestKind.ValidateNativeWriteRoundTrip,
            new GnuCashNativeWriteRoundTripRequest(sourceBookPath, workingBookPath, options.InstallPath),
            cancellationToken);
    }
}
