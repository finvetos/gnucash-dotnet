using GnuCash.DotNet.Options;
using GnuCash.DotNet.Protocol.Contracts;
using GnuCash.DotNet.Services;
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
}