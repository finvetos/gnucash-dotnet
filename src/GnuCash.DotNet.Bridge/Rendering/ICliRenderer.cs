using GnuCash.DotNet.Bridge.Commands;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Rendering;

public interface ICliRenderer
{
    void WriteHelp(IReadOnlyList<CommandDescriptor> commands);

    void WriteCommandList(IReadOnlyList<CommandDescriptor> commands);

    void WriteGnuCashValidation(GnuCashInstallationStatus status);

    void WriteNativeApiValidation(GnuCashNativeApiStatus status);

    void WriteNativeSessionValidation(GnuCashNativeSessionStatus status);

    void WriteJson<T>(T value);
}
