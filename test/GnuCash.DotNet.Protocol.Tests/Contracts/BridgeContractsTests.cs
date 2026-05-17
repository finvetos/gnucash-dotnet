using GnuCash.DotNet.Protocol.Contracts;
using Xunit;

namespace GnuCash.DotNet.Protocol.Tests.Contracts;

public sealed class BridgeContractsTests
{
    [Fact]
    public void RequestUsesCurrentProtocolVersion()
    {
        var request = new BridgeRequest(Guid.NewGuid(), BridgeRequestKind.Ping);

        Assert.Equal(BridgeProtocol.CurrentVersion, request.ProtocolVersion);
        Assert.Equal(BridgeRequestKind.Ping, request.Kind);
    }

    [Fact]
    public void ResponseCanCarryBridgeDiagnostics()
    {
        var response = new BridgeResponse(Guid.NewGuid(), false, DiagnosticOutput: "native stderr");

        Assert.Equal("native stderr", response.DiagnosticOutput);
    }

    [Fact]
    public void NativeSessionStatusCanCarryNativeSummaryCounts()
    {
        var status = new GnuCashNativeSessionStatus(
            IsReady: true,
            CanCallFromCurrentProcess: true,
            InstallPath: "C:/Program Files (x86)/gnucash",
            DisplayVersion: "5.13",
            BookPath: "sample.gnucash",
            EnginePath: "libgnc-engine.dll",
            SessionFilePath: "sample.gnucash",
            SessionUrl: "xml:///sample.gnucash",
            ProcessArchitecture: "X86",
            HasBook: true,
            HasRootAccount: true,
            AccountCount: 3,
            CommodityCount: 2,
            TransactionCount: 1,
            BackendErrorCode: 0,
            BackendErrorMessage: null,
            CheckedPaths: [],
            Message: "ready");

        Assert.Equal(3, status.AccountCount);
        Assert.Equal(2, status.CommodityCount);
        Assert.Equal(1, status.TransactionCount);
    }

    [Fact]
    public void BookCapabilityRequestsHaveStableProtocolNumbers()
    {
        Assert.Equal(2, (int)BridgeRequestKind.OpenBook);
        Assert.Equal(3, (int)BridgeRequestKind.ListAccounts);
        Assert.Equal(4, (int)BridgeRequestKind.Shutdown);
        Assert.Equal(5, (int)BridgeRequestKind.ListCommodities);
        Assert.Equal(6, (int)BridgeRequestKind.ListTransactions);
        Assert.Equal(7, (int)BridgeRequestKind.ListPrices);
        Assert.Equal(8, (int)BridgeRequestKind.ValidateNativeApi);
        Assert.Equal(9, (int)BridgeRequestKind.ValidateNativeSession);
    }
}
