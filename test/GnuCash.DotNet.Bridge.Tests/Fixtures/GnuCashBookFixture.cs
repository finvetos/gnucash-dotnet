using System.IO.Compression;
using System.Text;

namespace GnuCash.DotNet.Bridge.Tests.Fixtures;

internal sealed class GnuCashBookFixture : IDisposable
{
    private GnuCashBookFixture(string directoryPath, string bookPath)
    {
        DirectoryPath = directoryPath;
        BookPath = bookPath;
    }

    public string DirectoryPath { get; }

    public string BookPath { get; }

    public static GnuCashBookFixture CreateXml()
    {
        var directoryPath = CreateDirectory();
        var bookPath = Path.Combine(directoryPath, "sample.gnucash");
        File.WriteAllText(bookPath, SampleXml, Encoding.UTF8);

        return new GnuCashBookFixture(directoryPath, bookPath);
    }

    public static GnuCashBookFixture CreateCompressedXml()
    {
        var directoryPath = CreateDirectory();
        var bookPath = Path.Combine(directoryPath, "sample-compressed.gnucash");

        using var file = File.Create(bookPath);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        using var writer = new StreamWriter(gzip, Encoding.UTF8);
        writer.Write(SampleXml);

        return new GnuCashBookFixture(directoryPath, bookPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            "gnucash-dotnet-book-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        return directoryPath;
    }

    private const string SampleXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <gnc-v2
          xmlns:gnc="http://www.gnucash.org/XML/gnc"
          xmlns:book="http://www.gnucash.org/XML/book"
          xmlns:cmdty="http://www.gnucash.org/XML/cmdty"
          xmlns:act="http://www.gnucash.org/XML/act"
          xmlns:trn="http://www.gnucash.org/XML/trn"
          xmlns:price="http://www.gnucash.org/XML/price"
          xmlns:split="http://www.gnucash.org/XML/split"
          xmlns:ts="http://www.gnucash.org/XML/ts"
          xmlns:slot="http://www.gnucash.org/XML/slot">
          <gnc:book version="2.0.0">
            <book:id type="guid">aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</book:id>
            <gnc:commodity version="2.0.0">
              <cmdty:space>CURRENCY</cmdty:space>
              <cmdty:id>USD</cmdty:id>
              <cmdty:name>US Dollar</cmdty:name>
              <cmdty:xcode>840</cmdty:xcode>
              <cmdty:fraction>100</cmdty:fraction>
            </gnc:commodity>
            <gnc:commodity version="2.0.0">
              <cmdty:space>NASDAQ</cmdty:space>
              <cmdty:id>MSFT</cmdty:id>
              <cmdty:name>Microsoft</cmdty:name>
              <cmdty:fraction>10000</cmdty:fraction>
            </gnc:commodity>
            <gnc:pricedb version="1">
              <price>
                <price:id type="guid">66666666666666666666666666666666</price:id>
                <price:commodity>
                  <cmdty:space>NASDAQ</cmdty:space>
                  <cmdty:id>MSFT</cmdty:id>
                </price:commodity>
                <price:currency>
                  <cmdty:space>CURRENCY</cmdty:space>
                  <cmdty:id>USD</cmdty:id>
                </price:currency>
                <price:time>
                  <ts:date>2026-01-03 00:00:00 +0000</ts:date>
                </price:time>
                <price:source>user:price</price:source>
                <price:type>last</price:type>
                <price:value>25000/100</price:value>
              </price>
            </gnc:pricedb>
            <gnc:account version="2.0.0">
              <act:name>Root Account</act:name>
              <act:id type="guid">00000000000000000000000000000000</act:id>
              <act:type>ROOT</act:type>
              <act:slots>
                <slot>
                  <slot:key>placeholder</slot:key>
                  <slot:value type="string">true</slot:value>
                </slot>
              </act:slots>
            </gnc:account>
            <gnc:account version="2.0.0">
              <act:name>Checking</act:name>
              <act:id type="guid">11111111111111111111111111111111</act:id>
              <act:type>BANK</act:type>
              <act:commodity>
                <cmdty:space>CURRENCY</cmdty:space>
                <cmdty:id>USD</cmdty:id>
              </act:commodity>
              <act:parent type="guid">00000000000000000000000000000000</act:parent>
            </gnc:account>
            <gnc:account version="2.0.0">
              <act:name>Opening Balances</act:name>
              <act:id type="guid">22222222222222222222222222222222</act:id>
              <act:type>EQUITY</act:type>
              <act:commodity>
                <cmdty:space>CURRENCY</cmdty:space>
                <cmdty:id>USD</cmdty:id>
              </act:commodity>
              <act:parent type="guid">00000000000000000000000000000000</act:parent>
            </gnc:account>
            <gnc:transaction version="2.0.0">
              <trn:id type="guid">33333333333333333333333333333333</trn:id>
              <trn:currency>
                <cmdty:space>CURRENCY</cmdty:space>
                <cmdty:id>USD</cmdty:id>
              </trn:currency>
              <trn:num>DEP-1</trn:num>
              <trn:date-posted>
                <ts:date>2026-01-01 00:00:00 +0000</ts:date>
              </trn:date-posted>
              <trn:date-entered>
                <ts:date>2026-01-02 10:30:00 +0000</ts:date>
              </trn:date-entered>
              <trn:description>Opening deposit</trn:description>
              <trn:splits>
                <trn:split>
                  <split:id type="guid">44444444444444444444444444444444</split:id>
                  <split:reconciled-state>n</split:reconciled-state>
                  <split:value>100000/100</split:value>
                  <split:quantity>100000/100</split:quantity>
                  <split:account type="guid">11111111111111111111111111111111</split:account>
                </trn:split>
                <trn:split>
                  <split:id type="guid">55555555555555555555555555555555</split:id>
                  <split:memo>Initial funding</split:memo>
                  <split:reconciled-state>c</split:reconciled-state>
                  <split:value>-100000/100</split:value>
                  <split:quantity>-100000/100</split:quantity>
                  <split:account type="guid">22222222222222222222222222222222</split:account>
                </trn:split>
              </trn:splits>
            </gnc:transaction>
          </gnc:book>
        </gnc-v2>
        """;
}
