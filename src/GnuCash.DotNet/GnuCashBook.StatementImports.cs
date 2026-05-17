using System.Globalization;
using System.Text;
using GnuCash.DotNet.Models;

namespace GnuCash.DotNet;

public sealed partial class GnuCashBook
{
    /// <summary>
    /// Previews an OFX/QFX statement file without changing the book.
    /// </summary>
    public async Task<GnuCashOfxStatementPreview> PreviewOfxStatementImportAsync(
        string ofxPath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = NormalizeExistingImportPath(ofxPath);
        var text = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var transactions = SplitOfxTransactions(text)
            .Select(ParseOfxTransaction)
            .ToArray();

        return new GnuCashOfxStatementPreview(fullPath, transactions.Length, transactions);
    }

    /// <summary>
    /// Previews a QIF statement file without changing the book.
    /// </summary>
    public async Task<GnuCashQifStatementPreview> PreviewQifStatementImportAsync(
        string qifPath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = NormalizeExistingImportPath(qifPath);
        var lines = await File.ReadAllLinesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var accountType = lines.FirstOrDefault(line => line.StartsWith("!Type:", StringComparison.OrdinalIgnoreCase));
        var transactions = ParseQifTransactions(lines).ToArray();

        return new GnuCashQifStatementPreview(
            fullPath,
            accountType?["!Type:".Length..],
            transactions.Length,
            transactions);
    }

    private static string NormalizeExistingImportPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The import file does not exist.", fullPath);
        }

        return fullPath;
    }

    private static IEnumerable<string> SplitOfxTransactions(string text)
    {
        const string startTag = "<STMTTRN>";
        var index = 0;
        while ((index = text.IndexOf(startTag, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var next = text.IndexOf(startTag, index + startTag.Length, StringComparison.OrdinalIgnoreCase);
            var end = text.IndexOf("</STMTTRN>", index, StringComparison.OrdinalIgnoreCase);
            if (end >= 0 && (next < 0 || end < next))
            {
                end += "</STMTTRN>".Length;
                yield return text[index..end];
                index = end;
                continue;
            }

            yield return next < 0 ? text[index..] : text[index..next];
            index = next < 0 ? text.Length : next;
        }
    }

    private static GnuCashStatementPreviewTransaction ParseOfxTransaction(string text) =>
        new(
            ReadOfxValue(text, "FITID"),
            ParseOfxDate(ReadOfxValue(text, "DTPOSTED")),
            ReadOfxValue(text, "CHECKNUM"),
            ReadOfxValue(text, "NAME"),
            ReadOfxValue(text, "MEMO"),
            ParseDecimal(ReadOfxValue(text, "TRNAMT")));

    private static string? ReadOfxValue(string text, string tag)
    {
        var startToken = "<" + tag + ">";
        var start = text.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += startToken.Length;
        var endTag = "</" + tag + ">";
        var end = text.IndexOf(endTag, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            end = text.IndexOf('<', start);
        }

        if (end < 0)
        {
            end = text.Length;
        }

        return text[start..end].Trim();
    }

    private static DateOnly? ParseOfxDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            return null;
        }

        return DateOnly.TryParseExact(
            value[..8],
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static IEnumerable<GnuCashStatementPreviewTransaction> ParseQifTransactions(IReadOnlyList<string> lines)
    {
        var fields = new Dictionary<char, string>();
        foreach (var line in lines)
        {
            if (line.Length == 0 || line[0] == '!')
            {
                continue;
            }

            if (line[0] == '^')
            {
                yield return CreateQifTransaction(fields);
                fields.Clear();
                continue;
            }

            fields[line[0]] = line[1..].Trim();
        }

        if (fields.Count > 0)
        {
            yield return CreateQifTransaction(fields);
        }
    }

    private static GnuCashStatementPreviewTransaction CreateQifTransaction(
        IReadOnlyDictionary<char, string> fields) =>
        new(
            fields.GetValueOrDefault('N'),
            ParseQifDate(fields.GetValueOrDefault('D')),
            fields.GetValueOrDefault('N'),
            fields.GetValueOrDefault('P'),
            fields.GetValueOrDefault('M'),
            ParseDecimal(fields.GetValueOrDefault('T')));

    private static DateOnly? ParseQifDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "MM/dd/yyyy", "M/d/yyyy", "MM/dd'yy", "M/d'yy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(
            value,
            NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
}
