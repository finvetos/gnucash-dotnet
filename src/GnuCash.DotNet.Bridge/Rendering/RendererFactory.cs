namespace GnuCash.DotNet.Bridge.Rendering;

public sealed class RendererFactory
{
    public ICliRenderer Create(OutputMode mode, TextWriter writer) =>
        mode switch
        {
            OutputMode.Rich => new SpectreCliRenderer(writer),
            OutputMode.Json => new PlainCliRenderer(writer, json: true),
            _ => new PlainCliRenderer(writer)
        };
}
