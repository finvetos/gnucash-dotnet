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
}