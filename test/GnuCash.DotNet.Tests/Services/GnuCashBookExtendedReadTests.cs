using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookExtendedReadTests
{
    [Fact]
    public async Task CanReadExtendedXmlCapabilityAreas()
    {
        using var fixture = GnuCashBookFixture.Create();
        await AppendExtendedXmlAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var book = await CreateClient().OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);

        var lots = await book.ListLotsAsync(TestContext.Current.CancellationToken);
        var vendors = await book.ListVendorsAsync(TestContext.Current.CancellationToken);
        var documents = await book.ListBusinessDocumentsAsync(TestContext.Current.CancellationToken);
        var terms = await book.ListBillTermsAsync(TestContext.Current.CancellationToken);
        var taxTables = await book.ListTaxTablesAsync(TestContext.Current.CancellationToken);
        var scheduled = await book.ListScheduledTransactionsAsync(TestContext.Current.CancellationToken);
        var budgets = await book.ListBudgetsAsync(TestContext.Current.CancellationToken);
        var slots = await book.ListSlotsAsync(TestContext.Current.CancellationToken);

        Assert.Single(lots);
        Assert.Equal("Tax Lot", lots[0].Title);
        Assert.Single(vendors);
        Assert.Equal("Acme Supplies", vendors[0].Name);
        Assert.Single(documents);
        Assert.Equal("INV-1", documents[0].DocumentId);
        Assert.Single(terms);
        Assert.Equal("Net 30", terms[0].Name);
        Assert.Single(taxTables);
        Assert.Equal(1, taxTables[0].EntryCount);
        Assert.Single(scheduled);
        Assert.Equal("Monthly rent", scheduled[0].Name);
        Assert.Single(budgets);
        Assert.Equal("Operating Budget", budgets[0].Name);
        Assert.Contains(slots, slot => slot.Key == "vendor-note" && slot.Value == "Preferred");
    }

    private static async Task AppendExtendedXmlAsync(string bookPath, CancellationToken cancellationToken)
    {
        var xml = await File.ReadAllTextAsync(bookPath, cancellationToken);
        xml = xml.Replace(
            "</gnc:book>",
            """
              <lot>
                <guid>77777777777777777777777777777777</guid>
                <title>Tax Lot</title>
                <notes>Opened lot</notes>
                <split>44444444444444444444444444444444</split>
              </lot>
              <GncVendor>
                <guid>88888888888888888888888888888888</guid>
                <id>V-1</id>
                <name>Acme Supplies</name>
                <currency><space>CURRENCY</space><id>USD</id></currency>
                <slots><slot><key>vendor-note</key><value type="string">Preferred</value></slot></slots>
              </GncVendor>
              <GncInvoice>
                <guid>99999999999999999999999999999999</guid>
                <id>INV-1</id>
                <owner><id>V-1</id></owner>
                <currency><space>CURRENCY</space><id>USD</id></currency>
                <entries><entry>line-1</entry></entries>
              </GncInvoice>
              <GncBillTerm>
                <guid>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1</guid>
                <name>Net 30</name>
                <desc>Due in 30 days</desc>
              </GncBillTerm>
              <GncTaxTable>
                <guid>bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb1</guid>
                <name>GST</name>
                <entries><entry>tax-line</entry></entries>
              </GncTaxTable>
              <schedxaction>
                <guid>ccccccccccccccccccccccccccccccc1</guid>
                <name>Monthly rent</name>
                <enabled>true</enabled>
                <freqspec>monthly</freqspec>
              </schedxaction>
              <budget>
                <guid>ddddddddddddddddddddddddddddddd1</guid>
                <name>Operating Budget</name>
                <description>FY budget</description>
                <amounts><amount>100/1</amount></amounts>
              </budget>
            </gnc:book>
            """);
        await File.WriteAllTextAsync(bookPath, xml, cancellationToken);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location
            }));
}
