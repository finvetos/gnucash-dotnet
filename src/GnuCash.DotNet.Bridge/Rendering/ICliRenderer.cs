using GnuCash.DotNet.Bridge.Commands;

namespace GnuCash.DotNet.Bridge.Rendering;

public interface ICliRenderer
{
    void WriteHelp(IReadOnlyList<CommandDescriptor> commands);

    void WriteCommandList(IReadOnlyList<CommandDescriptor> commands);

    void WriteJson<T>(T value);
}