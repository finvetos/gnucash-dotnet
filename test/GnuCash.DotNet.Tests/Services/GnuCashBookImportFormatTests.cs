using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashBookImportFormatTests
{
    [Fact]
    public async Task CanPreviewOfxAndQifStatementImports()
    {
        using var fixture = GnuCashBookFixture.Create();
        var ofxPath = Path.Combine(fixture.DirectoryPath, "statement.ofx");
        var qifPath = Path.Combine(fixture.DirectoryPath, "statement.qif");
        await File.WriteAllTextAsync(
            ofxPath,
            "<OFX><BANKTRANLIST><STMTTRN><DTPOSTED>20260105120000<TRNAMT>-4.50<FITID>abc123<NAME>Coffee<MEMO>Card</STMTTRN></BANKTRANLIST></OFX>",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            qifPath,
            """
            !Type:Bank
            D01/06/2026
            T10.25
            N1002
            PGrocery
            MReceipt
            ^
            """,
            TestContext.Current.CancellationToken);
        var book = await CreateClient().OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);

        var ofx = await book.PreviewOfxStatementImportAsync(ofxPath, TestContext.Current.CancellationToken);
        var qif = await book.PreviewQifStatementImportAsync(qifPath, TestContext.Current.CancellationToken);

        var ofxTransaction = Assert.Single(ofx.Transactions);
        Assert.Equal("abc123", ofxTransaction.ExternalId);
        Assert.Equal(-4.50m, ofxTransaction.Amount);
        var qifTransaction = Assert.Single(qif.Transactions);
        Assert.Equal("Bank", qif.AccountType);
        Assert.Equal(10.25m, qifTransaction.Amount);
    }

    [Fact]
    public async Task CanPreviewAndApplyCsvPriceImportsToCopiedBook()
    {
        using var fixture = GnuCashBookFixture.Create();
        var csvPath = Path.Combine(fixture.DirectoryPath, "prices.csv");
        var workingPath = Path.Combine(fixture.DirectoryPath, "prices-copy.gnucash");
        await File.WriteAllTextAsync(
            csvPath,
            """
            CommoditySpace,CommodityId,CurrencySpace,CurrencyId,Date,Value
            NASDAQ,MSFT,CURRENCY,USD,2026-01-04,251.00
            """,
            TestContext.Current.CancellationToken);
        var client = CreateClient();
        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);

        var preview = await book.PreviewCsvPriceImportAsync(
            csvPath,
            new GnuCashCsvPriceImportOptions(),
            TestContext.Current.CancellationToken);
        var apply = await book.ApplyCsvPriceImportToCopiedBookAsync(
            csvPath,
            new GnuCashCsvPriceImportOptions(),
            workingPath,
            TestContext.Current.CancellationToken);
        var reopened = await client.OpenBookAsync(workingPath, TestContext.Current.CancellationToken);
        var prices = await reopened.ListPricesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, preview.ValidRows);
        Assert.True(apply.IsReady);
        Assert.Equal(1, apply.AppliedRowCount);
        Assert.Equal(2, prices.Count);
    }

    private static GnuCashClient CreateClient() =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions
            {
                BridgeExecutablePath = typeof(CliApplication).Assembly.Location
            }));
}
