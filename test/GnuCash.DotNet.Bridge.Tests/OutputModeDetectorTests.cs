using System.Collections;
using GnuCash.DotNet.Bridge.Rendering;
using Xunit;

namespace GnuCash.DotNet.Bridge.Tests;

public sealed class OutputModeDetectorTests
{
    private readonly OutputModeDetector detector = new();

    [Fact]
    public void UsesJsonWhenJsonFlagIsPresent()
    {
        var mode = detector.Detect(["list", "--json"], isOutputRedirected: false, new Hashtable());

        Assert.Equal(OutputMode.Json, mode);
    }

    [Fact]
    public void UsesPlainWhenOutputIsRedirected()
    {
        var mode = detector.Detect(["list"], isOutputRedirected: true, new Hashtable());

        Assert.Equal(OutputMode.Plain, mode);
    }

    [Fact]
    public void UsesRichWhenRichFormatIsExplicitEvenWhenOutputIsRedirected()
    {
        var mode = detector.Detect(["list", "--format", "rich"], isOutputRedirected: true, new Hashtable());

        Assert.Equal(OutputMode.Rich, mode);
    }

    [Fact]
    public void UsesPlainWhenNoColorIsPresent()
    {
        var environment = new Hashtable { ["NO_COLOR"] = "1" };

        var mode = detector.Detect(["list"], isOutputRedirected: false, environment);

        Assert.Equal(OutputMode.Plain, mode);
    }
}
