using System.Runtime.InteropServices;
using GnuCash.DotNet.Bridge.Discovery;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Native;

/// <summary>
/// Validates that the official GnuCash installation exposes the native engine symbols needed by the bridge.
/// </summary>
public sealed class GnuCashNativeApiProbe
{
    public static readonly IReadOnlyList<string> RequiredEngineExports =
    [
        "qof_session_new",
        "qof_session_begin",
        "qof_session_load",
        "qof_session_save",
        "qof_session_end",
        "qof_session_destroy",
        "qof_session_get_book",
        "qof_session_get_error",
        "qof_session_get_error_message",
        "gnc_book_get_root_account",
        "qof_book_get_counter",
        "qof_book_increment_and_format_counter",
        "xaccMallocTransaction",
        "xaccTransBeginEdit",
        "xaccTransCommitEdit",
        "xaccTransSetCurrency",
        "xaccTransSetDescription",
        "xaccTransSetDatePostedSecsNormalized",
        "xaccMallocSplit",
        "xaccSplitSetAccount",
        "xaccSplitSetParent",
        "xaccSplitSetValue",
        "xaccSplitSetAmount",
        "gnc_commodity_table_lookup",
        "gncCustomerCreate",
        "gncCustomerBeginEdit",
        "gncCustomerCommitEdit",
        "gncCustomerSetID",
        "gncCustomerSetName",
        "gncCustomerSetCurrency",
        "gncVendorCreate",
        "gncVendorBeginEdit",
        "gncVendorCommitEdit",
        "gncVendorSetID",
        "gncVendorSetName",
        "gncVendorSetCurrency",
        "gncEmployeeCreate",
        "gncEmployeeBeginEdit",
        "gncEmployeeCommitEdit",
        "gncEmployeeSetID",
        "gncEmployeeSetUsername",
        "gncEmployeeSetCurrency",
        "gncJobCreate",
        "gncJobBeginEdit",
        "gncJobCommitEdit",
        "gncJobSetID",
        "gncJobSetName",
        "gncJobSetOwner",
        "gncInvoiceCreate",
        "gncInvoiceBeginEdit",
        "gncInvoiceCommitEdit",
        "gncInvoiceSetID",
        "gncInvoiceSetOwner",
        "gncInvoiceSetCurrency",
        "gncInvoiceAddEntry",
        "gncInvoicePostToAccount",
        "gncInvoiceApplyPayment",
        "gncEntryCreate",
        "gncEntryBeginEdit",
        "gncEntryCommitEdit",
        "gncEntrySetDescription",
        "gncEntrySetInvAccount",
        "gncEntrySetInvPrice",
        "gncEntrySetBillAccount",
        "gncEntrySetBillPrice",
        "gncEntrySetDocQuantity",
        "gncTaxTableCreate",
        "gncTaxTableBeginEdit",
        "gncTaxTableCommitEdit",
        "gncTaxTableEntryCreate",
        "gncTaxTableEntrySetAccount",
        "gncTaxTableEntrySetAmount",
        "gncTaxTableEntrySetType",
        "gncBillTermCreate",
        "gncBillTermBeginEdit",
        "gncBillTermCommitEdit"
    ];

    private readonly GnuCashInstallationLocator locator;
    private readonly PortableExecutableExportReader exportReader;

    public GnuCashNativeApiProbe()
        : this(new GnuCashInstallationLocator(), new PortableExecutableExportReader())
    {
    }

    internal GnuCashNativeApiProbe(
        GnuCashInstallationLocator locator,
        PortableExecutableExportReader exportReader)
    {
        this.locator = locator;
        this.exportReader = exportReader;
    }

    public GnuCashNativeApiStatus Validate(string? explicitInstallPath = null)
    {
        var installation = locator.Validate(explicitInstallPath);
        var processArchitecture = RuntimeInformation.ProcessArchitecture.ToString();

        if (!installation.IsReady || string.IsNullOrWhiteSpace(installation.InstallPath))
        {
            return new GnuCashNativeApiStatus(
                IsReady: false,
                CanCallFromCurrentProcess: false,
                InstallPath: installation.InstallPath,
                DisplayVersion: installation.DisplayVersion,
                EnginePath: null,
                ProcessArchitecture: processArchitecture,
                RequiredExports: RequiredEngineExports,
                MissingExports: RequiredEngineExports,
                CheckedPaths: installation.CheckedPaths,
                Message: installation.Message);
        }

        var enginePath = Path.Combine(installation.InstallPath, "bin", "libgnc-engine.dll");
        IReadOnlySet<string> exports;
        try
        {
            exports = exportReader.ReadExportNames(enginePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new GnuCashNativeApiStatus(
                IsReady: false,
                CanCallFromCurrentProcess: false,
                InstallPath: installation.InstallPath,
                DisplayVersion: installation.DisplayVersion,
                EnginePath: enginePath,
                ProcessArchitecture: processArchitecture,
                RequiredExports: RequiredEngineExports,
                MissingExports: RequiredEngineExports,
                CheckedPaths: [.. installation.CheckedPaths, enginePath],
                Message: "GnuCash is installed, but the bridge could not inspect libgnc-engine.dll: " + ex.Message);
        }

        var missingExports = RequiredEngineExports
            .Where(requiredExport => !exports.Contains(requiredExport))
            .ToArray();
        var isReady = missingExports.Length == 0;
        var canCallFromCurrentProcess = isReady && RuntimeInformation.ProcessArchitecture == Architecture.X86;

        return new GnuCashNativeApiStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: canCallFromCurrentProcess,
            InstallPath: installation.InstallPath,
            DisplayVersion: installation.DisplayVersion,
            EnginePath: enginePath,
            ProcessArchitecture: processArchitecture,
            RequiredExports: RequiredEngineExports,
            MissingExports: missingExports,
            CheckedPaths: [.. installation.CheckedPaths, enginePath],
            Message: CreateMessage(isReady, canCallFromCurrentProcess, missingExports));
    }

    private static string CreateMessage(
        bool isReady,
        bool canCallFromCurrentProcess,
        IReadOnlyList<string> missingExports)
    {
        if (!isReady)
        {
            return "The installed GnuCash engine is missing required native API exports: " +
                   string.Join(", ", missingExports);
        }

        if (!canCallFromCurrentProcess)
        {
            return "The installed GnuCash engine exposes the required native API. " +
                   "Runtime calls must be made from the packaged win-x86 bridge process.";
        }

        return "The installed GnuCash engine exposes the required native API and this process can call it.";
    }
}
