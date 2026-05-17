namespace GnuCash.DotNet.Models;

/// <summary>
/// Local GnuCash book backend type detected from an on-disk file.
/// </summary>
public enum GnuCashBookBackendKind
{
    Unknown = 0,
    Xml = 1,
    CompressedXml = 2,
    SQLite = 3
}

/// <summary>
/// Local book backend classification result.
/// </summary>
public sealed record GnuCashBookBackendClassification(
    string BookPath,
    GnuCashBookBackendKind Kind,
    string Description,
    bool CanUseXmlReader,
    bool CanUseNativeBridge,
    long FileSizeBytes);

/// <summary>
/// Result of creating a local backup copy of a GnuCash book.
/// </summary>
public sealed record GnuCashBookBackupResult(
    string SourceBookPath,
    string BackupBookPath,
    long BytesCopied,
    DateTimeOffset CreatedAt);

/// <summary>
/// Result of restoring a local GnuCash book backup.
/// </summary>
public sealed record GnuCashBookRestoreResult(
    string BackupBookPath,
    string DestinationBookPath,
    long BytesCopied,
    DateTimeOffset RestoredAt);
