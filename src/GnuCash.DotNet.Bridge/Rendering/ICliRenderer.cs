using GnuCash.DotNet.Bridge.Commands;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Rendering;

public interface ICliRenderer
{
    void WriteHelp(IReadOnlyList<CommandDescriptor> commands);

    void WriteCommandList(IReadOnlyList<CommandDescriptor> commands);

    void WriteGnuCashValidation(GnuCashInstallationStatus status);

    void WriteNativeApiValidation(GnuCashNativeApiStatus status);

    void WriteNativeExportInventory(GnuCashNativeExportInventoryStatus status);

    void WriteNativeSessionValidation(GnuCashNativeSessionStatus status);

    void WriteNativeReadParityValidation(GnuCashNativeReadParityStatus status);

    void WriteNativeWriteRoundTripValidation(GnuCashNativeWriteRoundTripStatus status);

    void WriteNativeCustomerWriteValidation(GnuCashNativeCustomerWriteStatus status);

    void WriteJson<T>(T value);
}
