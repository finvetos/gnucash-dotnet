using System.IO.Compression;
using System.Xml.Linq;

namespace GnuCash.DotNet.Xml;

internal sealed class GnuCashXmlBookDocument
{
    private GnuCashXmlBookDocument(string path, XDocument document, XElement book)
    {
        Path = path;
        Document = document;
        Book = book;
    }

    public string Path { get; }

    public XDocument Document { get; }

    public XElement Book { get; }

    public static XNamespace Gnc { get; } = "http://www.gnucash.org/XML/gnc";
    public static XNamespace BookNs { get; } = "http://www.gnucash.org/XML/book";
    public static XNamespace Cmdty { get; } = "http://www.gnucash.org/XML/cmdty";
    public static XNamespace Act { get; } = "http://www.gnucash.org/XML/act";
    public static XNamespace Trn { get; } = "http://www.gnucash.org/XML/trn";
    public static XNamespace Split { get; } = "http://www.gnucash.org/XML/split";
    public static XNamespace Price { get; } = "http://www.gnucash.org/XML/price";
    public static XNamespace Ts { get; } = "http://www.gnucash.org/XML/ts";
    public static XNamespace Slot { get; } = "http://www.gnucash.org/XML/slot";

    public static GnuCashXmlBookDocument Load(string bookPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);

        var fullPath = System.IO.Path.GetFullPath(bookPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The GnuCash book file does not exist.", fullPath);
        }

        using var stream = OpenRead(fullPath);
        var document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        var book = document
            .Descendants()
            .FirstOrDefault(element => HasLocalName(element, "book")) ??
            throw new InvalidOperationException("The file does not look like a GnuCash XML book.");

        return new GnuCashXmlBookDocument(fullPath, document, book);
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        Document.Save(path);
    }

    public IEnumerable<XElement> BookChildren(string localName) =>
        Book.Elements().Where(element => HasLocalName(element, localName));

    public XElement? FirstBookChild(string localName) =>
        Book.Elements().FirstOrDefault(element => HasLocalName(element, localName));

    public static bool HasLocalName(XElement element, string localName) =>
        string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal);

    public static XElement? Child(XElement? element, string localName) =>
        element?.Elements().FirstOrDefault(candidate => HasLocalName(candidate, localName));

    public static string? ChildText(XElement? element, string localName) =>
        Child(element, localName)?.Value.Trim();

    public static string GuidText() => Guid.NewGuid().ToString("N");

    private static Stream OpenRead(string fullPath)
    {
        var file = File.OpenRead(fullPath);
        Span<byte> header = stackalloc byte[16];
        var read = file.Read(header);
        file.Position = 0;

        if (read >= 2 && header[0] == 0x1f && header[1] == 0x8b)
        {
            return new GZipStream(file, CompressionMode.Decompress);
        }

        if (read >= 16 && IsSqliteHeader(header))
        {
            file.Dispose();
            throw new InvalidOperationException("SQLite books require the native bridge and cannot be edited as XML.");
        }

        return file;
    }

    private static bool IsSqliteHeader(ReadOnlySpan<byte> header)
    {
        const string sqliteHeader = "SQLite format 3";
        return string.Equals(
            System.Text.Encoding.ASCII.GetString(header[..sqliteHeader.Length]),
            sqliteHeader,
            StringComparison.Ordinal);
    }
}
