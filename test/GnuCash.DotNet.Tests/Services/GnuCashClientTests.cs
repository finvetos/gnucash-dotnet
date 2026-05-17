using GnuCash.DotNet.Bridge;
using GnuCash.DotNet.Models;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Protocol.Contracts;
using GnuCash.DotNet.Reports;
using GnuCash.DotNet.Services;
using GnuCash.DotNet.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace GnuCash.DotNet.Tests.Services;

public sealed class GnuCashClientTests
{
    [Fact]
    public void CreatesPingRequestForCurrentProtocol()
    {
        var client = new GnuCashClient(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(new GnuCashBridgeOptions()));

        var request = client.CreatePingRequest();

        Assert.Equal(BridgeRequestKind.Ping, request.Kind);
        Assert.Equal(BridgeProtocol.CurrentVersion, request.ProtocolVersion);
    }

    [Fact]
    public async Task ValidateInstallationAsyncUsesBridgeHeadlessProtocol()
    {
        using var fixture = GnuCashInstallFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
            InstallPath = fixture.InstallPath
        });

        var status = await client.ValidateInstallationAsync(TestContext.Current.CancellationToken);

        Assert.True(status.IsReady);
        Assert.Equal(fixture.InstallPath, status.InstallPath);
        Assert.Contains(fixture.InstallPath, status.CheckedPaths);
    }

    [Fact]
    public async Task ValidateInstallationAsyncReturnsNotReadyStatusWhenGnuCashIsMissing()
    {
        var installPath = Path.Combine(Path.GetTempPath(), "gnucash-dotnet-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installPath);
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location,
            InstallPath = installPath
        });

        try
        {
            var status = await client.ValidateInstallationAsync(TestContext.Current.CancellationToken);

            Assert.False(status.IsReady);
            Assert.Contains("Install GnuCash for Windows first", status.Message, StringComparison.Ordinal);
            Assert.NotEmpty(status.MissingPaths);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateInstallationAsyncFailsClearlyWhenBridgeCannotBeFound()
    {
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.exe")
        });

        var error = await Assert.ThrowsAsync<FileNotFoundException>(
            () => client.ValidateInstallationAsync(TestContext.Current.CancellationToken));

        Assert.Contains("BridgeExecutablePath does not exist", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenBookAsyncReturnsReadOnlyBookHandleUsingBridgeProtocol()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);

        Assert.Equal(Path.GetFullPath(fixture.BookPath), book.BookPath);
        Assert.Equal("GnuCashXml", book.Info.FileFormat);
        Assert.Equal(3, book.Info.AccountCount);
        Assert.Equal(2, book.Info.CommodityCount);
        Assert.Equal(1, book.Info.TransactionCount);
        Assert.Equal(2, book.Info.SplitCount);
        Assert.Equal(1, book.Info.PriceCount);
    }

    [Fact]
    public async Task OpenBookAsyncIncludesBridgeDiagnosticsWhenBridgeWritesStandardError()
    {
        var missingBookPath = Path.Combine(
            Path.GetTempPath(),
            "gnucash-dotnet-tests",
            Guid.NewGuid().ToString("N"),
            "missing.gnucash");
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.OpenBookAsync(missingBookPath, TestContext.Current.CancellationToken));

        Assert.Contains("does not exist", error.Message, StringComparison.Ordinal);
        Assert.Contains("Bridge diagnostics:", error.Message, StringComparison.Ordinal);
        Assert.Contains("Bridge request failed:", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenBookAsyncCanListCommoditiesAccountsTransactionsAndSplits()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var commodities = await book.ListCommoditiesAsync(TestContext.Current.CancellationToken);
        var accounts = await book.ListAccountsAsync(TestContext.Current.CancellationToken);
        var transactions = await book.ListTransactionsAsync(TestContext.Current.CancellationToken);

        Assert.Contains(commodities, commodity =>
            commodity.Space == "CURRENCY" &&
            commodity.Id == "USD");
        Assert.Contains(accounts, account =>
            account.Name == "Checking" &&
            account.Type == "BANK" &&
            account.ParentId == "00000000000000000000000000000000");

        var transaction = Assert.Single(transactions);
        Assert.Equal("Opening deposit", transaction.Description);
        Assert.Equal("USD", transaction.CurrencyId);
        Assert.Equal(2, transaction.Splits.Count);
        Assert.Contains(transaction.Splits, split =>
            split.AccountId == "11111111111111111111111111111111" &&
            split.Value.RawValue == "100000/100" &&
            split.Value.Numerator == 100000);
    }

    [Fact]
    public async Task OpenBookAsyncCanListSecuritiesAndPrices()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var securities = await book.ListSecuritiesAsync(TestContext.Current.CancellationToken);
        var prices = await book.ListPricesAsync(
            new GnuCashPriceQuery(CommoditySpace: "NASDAQ", CommodityId: "MSFT", CurrencyId: "USD"),
            TestContext.Current.CancellationToken);
        var latest = await book.ListLatestPricesAsync(TestContext.Current.CancellationToken);

        var security = Assert.Single(securities);
        Assert.Equal("NASDAQ", security.Space);
        Assert.Equal("MSFT", security.Id);
        var price = Assert.Single(prices);
        Assert.Equal("25000/100", price.Value.RawValue);
        Assert.Equal(250m, price.Value.DecimalValue);
        Assert.Single(latest);
    }

    [Fact]
    public async Task OpenBookAsyncCanFilterAccountsAndTransactions()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var bankAccounts = await book.ListAccountsAsync(
            new GnuCashAccountQuery(Type: "BANK", NameContains: "check"),
            TestContext.Current.CancellationToken);
        var checking = await book.GetAccountByIdAsync(
            "11111111111111111111111111111111",
            TestContext.Current.CancellationToken);
        var transactions = await book.ListTransactionsAsync(
            new GnuCashTransactionQuery(
                AccountId: "11111111111111111111111111111111",
                PostedFrom: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PostedTo: new DateTimeOffset(2026, 1, 31, 23, 59, 59, TimeSpan.Zero),
                DescriptionContains: "deposit",
                CurrencyId: "USD",
                ReconciledState: "n"),
            TestContext.Current.CancellationToken);
        var reconciledCheckingTransactions = await book.ListTransactionsAsync(
            new GnuCashTransactionQuery(
                AccountId: "11111111111111111111111111111111",
                ReconciledState: "c"),
            TestContext.Current.CancellationToken);

        var bankAccount = Assert.Single(bankAccounts);
        Assert.Equal("Checking", bankAccount.Name);
        Assert.NotNull(checking);
        Assert.Equal("BANK", checking!.Type);
        Assert.Single(transactions);
        Assert.Empty(reconciledCheckingTransactions);
    }

    [Fact]
    public async Task OpenBookAsyncCanCalculateAccountBalances()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var balances = await book.ListAccountBalancesAsync(TestContext.Current.CancellationToken);
        var checking = await book.GetAccountBalanceAsync(
            "11111111111111111111111111111111",
            TestContext.Current.CancellationToken);
        var clearedEquity = await book.GetAccountBalanceAsync(
            "22222222222222222222222222222222",
            new GnuCashTransactionQuery(ReconciledState: "c"),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, balances.Count);
        Assert.NotNull(checking);
        Assert.Equal("100000/100", checking!.Balance.RawValue);
        Assert.Equal(1000m, checking.Balance.DecimalValue);
        Assert.Equal(1, checking.SplitCount);
        Assert.NotNull(clearedEquity);
        Assert.Equal("-100000/100", clearedEquity!.Balance.RawValue);
        Assert.Equal(-1000m, clearedEquity.Balance.DecimalValue);
    }

    [Fact]
    public async Task OpenBookAsyncCanCreateAccountSummaryReport()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var report = await book.CreateAccountSummaryReportAsync(TestContext.Current.CancellationToken);
        var csv = GnuCashReportSerializer.ToCsv(report);
        var json = GnuCashReportSerializer.ToJson(report);

        Assert.Equal(3, report.Rows.Count);
        Assert.Contains(report.Rows, row =>
            row.AccountName == "Checking" &&
            row.AccountType == "BANK" &&
            row.Balance.RawValue == "100000/100");
        Assert.Contains(report.Totals, total =>
            total.AccountType == "EQUITY" &&
            total.Balance.RawValue == "-100000/100");
        Assert.Contains("AccountId,AccountName,AccountType", csv, StringComparison.Ordinal);
        Assert.Contains("Checking", csv, StringComparison.Ordinal);
        Assert.Contains("\"Rows\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenBookAsyncCanCreateTransactionReport()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var fullReport = await book.CreateTransactionReportAsync(TestContext.Current.CancellationToken);
        var checkingReport = await book.CreateTransactionReportAsync(
            new GnuCashTransactionQuery(AccountId: "11111111111111111111111111111111"),
            TestContext.Current.CancellationToken);
        var csv = GnuCashReportSerializer.ToCsv(checkingReport);

        Assert.Equal(2, fullReport.Rows.Count);
        var row = Assert.Single(checkingReport.Rows);
        Assert.Equal("Opening deposit", row.Description);
        Assert.Equal("Checking", row.AccountName);
        Assert.Equal("100000/100", row.Value.RawValue);
        Assert.Contains("TransactionId,PostedAt,Number,Description", csv, StringComparison.Ordinal);
        Assert.Contains("Opening deposit", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenBookAsyncCanSummarizeReconciliationStates()
    {
        using var fixture = GnuCashBookFixture.Create();
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var summaries = await book.ListReconciliationSummariesAsync(TestContext.Current.CancellationToken);
        var checking = await book.GetReconciliationSummaryAsync(
            "11111111111111111111111111111111",
            TestContext.Current.CancellationToken);
        var clearedOnlyEquity = await book.GetReconciliationSummaryAsync(
            "22222222222222222222222222222222",
            new GnuCashTransactionQuery(ReconciledState: "c"),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, summaries.Count);
        Assert.NotNull(checking);
        Assert.Equal(1, checking!.UnreconciledSplitCount);
        Assert.Equal(0, checking.ClearedSplitCount);
        Assert.Equal("100000/100", checking.UnreconciledBalance.RawValue);
        Assert.NotNull(clearedOnlyEquity);
        Assert.Equal(1, clearedOnlyEquity!.ClearedSplitCount);
        Assert.Equal("-100000/100", clearedOnlyEquity.ClearedBalance.RawValue);
        Assert.Equal(0, clearedOnlyEquity.UnreconciledSplitCount);
    }

    [Fact]
    public async Task OpenBookAsyncCanPreviewCsvTransactionImport()
    {
        using var fixture = GnuCashBookFixture.Create();
        var csvPath = Path.Combine(fixture.DirectoryPath, "statement.csv");
        await File.WriteAllTextAsync(
            csvPath,
            """
            Date,Description,Amount,Number,Memo
            2026-01-05,"Coffee, shop",-4.50,1001,Debit card
            not-a-date,Bad row,nope,1002,
            """,
            TestContext.Current.CancellationToken);
        var client = CreateClient(new GnuCashBridgeOptions
        {
            BridgeExecutablePath = typeof(CliApplication).Assembly.Location
        });

        var book = await client.OpenBookAsync(fixture.BookPath, TestContext.Current.CancellationToken);
        var preview = await book.PreviewCsvTransactionImportAsync(
            csvPath,
            new GnuCashCsvTransactionImportOptions(
                AccountId: "11111111111111111111111111111111",
                NumberColumn: "Number",
                MemoColumn: "Memo"),
            TestContext.Current.CancellationToken);

        Assert.Equal(Path.GetFullPath(csvPath), preview.SourcePath);
        Assert.Equal(2, preview.TotalRows);
        Assert.Equal(1, preview.ValidRows);
        Assert.Equal(1, preview.InvalidRows);
        var validRow = Assert.Single(preview.Rows, row => row.IsValid);
        Assert.Equal(new DateOnly(2026, 1, 5), validRow.PostedDate);
        Assert.Equal("Coffee, shop", validRow.Description);
        Assert.Equal("-450/100", validRow.Value!.RawValue);
        Assert.Equal(-4.50m, validRow.Value.DecimalValue);
        Assert.Contains(preview.Issues, issue => issue.Code == "InvalidDate");
        Assert.Contains(preview.Issues, issue => issue.Code == "InvalidAmount");
    }

    private static GnuCashClient CreateClient(GnuCashBridgeOptions options) =>
        new(
            NullLogger<GnuCashClient>.Instance,
            OptionsFactory.Create(options));

    private sealed class GnuCashInstallFixture : IDisposable
    {
        private GnuCashInstallFixture(string installPath)
        {
            InstallPath = installPath;
        }

        public string InstallPath { get; }

        public static GnuCashInstallFixture Create()
        {
            var installPath = Path.Combine(
                Path.GetTempPath(),
                "gnucash-dotnet-tests",
                Guid.NewGuid().ToString("N"));

            CreateFile(installPath, "bin", "gnucash.exe");
            CreateFile(installPath, "bin", "gnucash-cli.exe");
            CreateFile(installPath, "bin", "libgnc-core-utils.dll");
            CreateFile(installPath, "bin", "libgnc-engine.dll");
            CreateFile(installPath, "bin", "libgnc-module.dll");
            Directory.CreateDirectory(Path.Combine(installPath, "etc", "gnucash"));
            Directory.CreateDirectory(Path.Combine(installPath, "lib", "gnucash"));
            Directory.CreateDirectory(Path.Combine(installPath, "share", "gnucash"));

            return new GnuCashInstallFixture(installPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(InstallPath))
            {
                Directory.Delete(InstallPath, recursive: true);
            }
        }

        private static void CreateFile(string root, params string[] parts)
        {
            var path = Path.Combine([root, .. parts]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
        }
    }
}
