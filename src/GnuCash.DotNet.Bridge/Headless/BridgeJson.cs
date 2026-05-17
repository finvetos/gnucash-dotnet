using System.Text.Json;

namespace GnuCash.DotNet.Bridge.Headless;

internal static class BridgeJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
