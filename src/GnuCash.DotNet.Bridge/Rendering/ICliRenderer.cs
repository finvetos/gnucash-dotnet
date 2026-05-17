using GnuCash.DotNet.Bridge.Commands;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Rendering;

public interface ICliRenderer
{
    void WriteHelp(IReadOnlyList<CommandDescriptor> commands);

    void WriteCommandList(IReadOnlyList<CommandDescriptor> commands);

    void WriteGnuCashValidation(GnuCashInstallationStatus status);

    void WriteJson<T>(T value);
}
