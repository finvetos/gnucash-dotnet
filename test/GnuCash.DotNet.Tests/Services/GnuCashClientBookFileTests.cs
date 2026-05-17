using System.IO.Compression;
using System.Text;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashClientBookFileTests
{
    [Fact]
    public void ClassifyBookBackendDetectsXmlCompressedXmlAndSqlite()
    {
        using var fixture = GnuCashBookFixture.Create();
        var compressedPath = Path.Combine(fixture.DirectoryPath, "compressed.gnucash");
        var sqlitePath = Path.Combine(fixture.DirectoryPath, "book.sqlite");
        CreateGzipBook(compressedPath);
        File.WriteAllText(sqlitePath, "SQLite format 3\0", Encoding.ASCII);
        var client = CreateClient();

        var xml = client.ClassifyBookBackend(fixture.BookPath);
        var compressed = client.ClassifyBookBackend(compressedPath);
        var sqlite = client.ClassifyBookBackend(sqlitePath);

        Assert.Equal(GnuCashBookBackendKind.Xml, xml.Kind);
        Assert.True(xml.CanUseXmlReader);
        Assert.Equal(GnuCashBookBackendKind.CompressedXml, compressed.Kind);
        Assert.True(compressed.CanUseXmlReader);
        Assert.Equal(GnuCashBookBackendKind.SQLite, sqlite.Kind);
        Assert.False(sqlite.CanUseXmlReader);
        Assert.True(sqlite.CanUseNativeBridge);
    }

    [Fact]
    public async Task BackupAndRestoreBookCopiesBytesWithoutChangingSource()
    {
        using var fixture = GnuCashBookFixture.Create();
        var backupDirectory = Path.Combine(fixture.DirectoryPath, "backups");
        var restorePath = Path.Combine(fixture.DirectoryPath, "restored.gnucash");
        var sourceText = await File.ReadAllTextAsync(
            fixture.BookPath,
            TestContext.Current.CancellationToken);
        var client = CreateClient();

        var backup = await client.BackupBookAsync(
            fixture.BookPath,
            backupDirectory,
            cancellationToken: TestContext.Current.CancellationToken);
        var restore = await client.RestoreBookBackupAsync(
            backup.BackupBookPath,
            restorePath,
            cancellationToken: TestContext.Current.CancellationToken);
        var restoredText = await File.ReadAllTextAsync(
            restore.DestinationBookPath,
            TestContext.Current.CancellationToken);

        Assert.True(File.Exists(backup.BackupBookPath));
        Assert.Equal(Path.GetFullPath(fixture.BookPath), backup.SourceBookPath);
        Assert.True(backup.BytesCopied > 0);
        Assert.Equal(Path.GetFullPath(restorePath), restore.DestinationBookPath);
        Assert.Equal(sourceText, restoredText);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions()));

    private static void CreateGzipBook(string path)
    {
        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        var bytes = Encoding.UTF8.GetBytes("<gnc-v2 />");
        gzip.Write(bytes);
    }
}
