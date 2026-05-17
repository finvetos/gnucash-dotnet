using GnuCash.DotNet.Models;
using GnuCash.DotNet.Xml;
using System.Xml.Linq;

namespace GnuCash.DotNet.Services;

public sealed partial class GnuCashClient
{
    private static readonly byte[] GZipMagic = [0x1F, 0x8B];
    private const string SQLiteHeader = "SQLite format 3";

    /// <summary>
    /// Creates a new XML GnuCash book and opens it through the SDK.
    /// </summary>
    public async Task<GnuCashBook> CreateBookAsync(
        string bookPath,
        GnuCashBookCreateOptions? options = null,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);

        var createOptions = options ?? new GnuCashBookCreateOptions();
        var fullPath = Path.GetFullPath(bookPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using (var stream = new FileStream(fullPath, mode, FileAccess.Write, FileShare.None))
        {
            CreateBookDocument(createOptions).Save(stream);
        }

        return await OpenBookAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Classifies a local book file by inspecting its on-disk signature.
    /// </summary>
    public GnuCashBookBackendClassification ClassifyBookBackend(string bookPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);

        var fullPath = Path.GetFullPath(bookPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The GnuCash book file does not exist.", fullPath);
        }

        var fileInfo = new FileInfo(fullPath);
        var kind = DetectBookBackend(fullPath);

        return new GnuCashBookBackendClassification(
            fullPath,
            kind,
            DescribeBackend(kind),
            CanUseXmlReader(kind),
            CanUseNativeBridge(kind),
            fileInfo.Length);
    }

    /// <summary>
    /// Copies a book to a timestamped backup file without modifying the source book.
    /// </summary>
    public async Task<GnuCashBookBackupResult> BackupBookAsync(
        string bookPath,
        string? backupDirectory = null,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);

        var sourcePath = Path.GetFullPath(bookPath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The GnuCash book file does not exist.", sourcePath);
        }

        var destinationDirectory = string.IsNullOrWhiteSpace(backupDirectory)
            ? Path.GetDirectoryName(sourcePath)!
            : Path.GetFullPath(backupDirectory);
        Directory.CreateDirectory(destinationDirectory);

        var createdAt = DateTimeOffset.UtcNow;
        var destinationPath = CreateBackupPath(sourcePath, destinationDirectory, createdAt, overwrite);
        await CopyFileAsync(sourcePath, destinationPath, overwrite, cancellationToken).ConfigureAwait(false);

        return new GnuCashBookBackupResult(
            sourcePath,
            destinationPath,
            new FileInfo(destinationPath).Length,
            createdAt);
    }

    /// <summary>
    /// Restores a book backup to a target path.
    /// </summary>
    public async Task<GnuCashBookRestoreResult> RestoreBookBackupAsync(
        string backupPath,
        string destinationBookPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationBookPath);

        var sourcePath = Path.GetFullPath(backupPath);
        var destinationPath = Path.GetFullPath(destinationBookPath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The GnuCash backup file does not exist.", sourcePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await CopyFileAsync(sourcePath, destinationPath, overwrite, cancellationToken).ConfigureAwait(false);

        return new GnuCashBookRestoreResult(
            sourcePath,
            destinationPath,
            new FileInfo(destinationPath).Length,
            DateTimeOffset.UtcNow);
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var destination = new FileStream(destinationPath, fileMode, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    private static string CreateBackupPath(
        string sourcePath,
        string backupDirectory,
        DateTimeOffset createdAt,
        bool overwrite)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        var timestamp = createdAt.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var path = Path.Combine(backupDirectory, $"{fileName}.{timestamp}.backup{extension}");

        if (overwrite || !File.Exists(path))
        {
            return path;
        }

        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(backupDirectory, $"{fileName}.{timestamp}.{index}.backup{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static GnuCashBookBackendKind DetectBookBackend(string bookPath)
    {
        Span<byte> buffer = stackalloc byte[512];
        using var stream = File.OpenRead(bookPath);
        var length = stream.Read(buffer);
        var bytes = buffer[..length];

        if (bytes.Length >= 2 && bytes[0] == GZipMagic[0] && bytes[1] == GZipMagic[1])
        {
            return GnuCashBookBackendKind.CompressedXml;
        }

        var header = System.Text.Encoding.UTF8.GetString(bytes);
        if (header.StartsWith(SQLiteHeader, StringComparison.Ordinal))
        {
            return GnuCashBookBackendKind.SQLite;
        }

        var trimmed = header.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (trimmed.StartsWith("<?xml", StringComparison.Ordinal) ||
            trimmed.StartsWith("<gnc-v2", StringComparison.Ordinal))
        {
            return GnuCashBookBackendKind.Xml;
        }

        var extension = Path.GetExtension(bookPath);
        return extension.Equals(".sqlite", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".db", StringComparison.OrdinalIgnoreCase)
            ? GnuCashBookBackendKind.SQLite
            : GnuCashBookBackendKind.Unknown;
    }

    private static bool CanUseXmlReader(GnuCashBookBackendKind kind) =>
        kind is GnuCashBookBackendKind.Xml or GnuCashBookBackendKind.CompressedXml;

    private static bool CanUseNativeBridge(GnuCashBookBackendKind kind) =>
        kind is not GnuCashBookBackendKind.Unknown;

    private static string DescribeBackend(GnuCashBookBackendKind kind) =>
        kind switch
        {
            GnuCashBookBackendKind.Xml => "Uncompressed GnuCash XML book.",
            GnuCashBookBackendKind.CompressedXml => "GZip-compressed GnuCash XML book.",
            GnuCashBookBackendKind.SQLite => "SQLite-backed GnuCash book.",
            _ => "Unknown local book format."
        };

    private static XDocument CreateBookDocument(GnuCashBookCreateOptions options)
    {
        var rootAccountId = GnuCashXmlBookDocument.GuidText();

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                GnuCashXmlBookDocument.Gnc + "v2",
                new XAttribute(XNamespace.Xmlns + "gnc", GnuCashXmlBookDocument.Gnc),
                new XAttribute(XNamespace.Xmlns + "book", GnuCashXmlBookDocument.BookNs),
                new XAttribute(XNamespace.Xmlns + "cmdty", GnuCashXmlBookDocument.Cmdty),
                new XAttribute(XNamespace.Xmlns + "act", GnuCashXmlBookDocument.Act),
                new XAttribute(XNamespace.Xmlns + "slot", GnuCashXmlBookDocument.Slot),
                new XElement(
                    GnuCashXmlBookDocument.Gnc + "book",
                    new XAttribute("version", "2.0.0"),
                    new XElement(GnuCashXmlBookDocument.BookNs + "id", new XAttribute("type", "guid"), GnuCashXmlBookDocument.GuidText()),
                    new XElement(
                        GnuCashXmlBookDocument.Gnc + "commodity",
                        new XAttribute("version", "2.0.0"),
                        new XElement(GnuCashXmlBookDocument.Cmdty + "space", options.CurrencySpace),
                        new XElement(GnuCashXmlBookDocument.Cmdty + "id", options.CurrencyId),
                        new XElement(GnuCashXmlBookDocument.Cmdty + "name", options.CurrencyName),
                        new XElement(GnuCashXmlBookDocument.Cmdty + "fraction", options.CurrencyFraction)),
                    new XElement(
                        GnuCashXmlBookDocument.Gnc + "account",
                        new XAttribute("version", "2.0.0"),
                        new XElement(GnuCashXmlBookDocument.Act + "name", options.RootAccountName),
                        new XElement(GnuCashXmlBookDocument.Act + "id", new XAttribute("type", "guid"), rootAccountId),
                        new XElement(GnuCashXmlBookDocument.Act + "type", "ROOT")))));
    }
}
