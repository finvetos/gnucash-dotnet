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
    public void BookCapabilityRequestsHaveStableProtocolNumbers()
    {
        Assert.Equal(2, (int)BridgeRequestKind.OpenBook);
        Assert.Equal(3, (int)BridgeRequestKind.ListAccounts);
        Assert.Equal(4, (int)BridgeRequestKind.Shutdown);
        Assert.Equal(5, (int)BridgeRequestKind.ListCommodities);
        Assert.Equal(6, (int)BridgeRequestKind.ListTransactions);
        Assert.Equal(7, (int)BridgeRequestKind.ListPrices);
        Assert.Equal(8, (int)BridgeRequestKind.ValidateNativeApi);
    }
}
