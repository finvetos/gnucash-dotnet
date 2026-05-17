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
        "qof_book_new",
        "qof_session_begin",
        "qof_session_load",
        "qof_session_save",
        "qof_session_end",
        "qof_session_destroy",
        "qof_session_get_book",
        "qof_session_get_error",
        "qof_session_get_error_message",
        "qof_session_get_file_path",
        "qof_session_get_url",
        "gnc_uri_normalize_uri",
        "gnc_engine_init",
        "gnc_engine_shutdown",
        "gnc_book_get_root_account",
        "gnc_book_count_transactions",
        "gnc_account_n_descendants",
        "gnc_commodity_table_get_table",
        "gnc_commodity_table_get_size",
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

    public static readonly IReadOnlyList<string> RequiredModuleExports =
    [
        "gnc_module_system_init"
    ];

    public static readonly IReadOnlyList<string> RequiredCoreUtilsExports =
    [
        "gnc_gbr_init",
        "gnc_environment_setup"
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
            return CreateInstallNotReadyStatus(installation, processArchitecture);
        }

        var paths = NativeApiProbePaths.Create(installation.InstallPath);
        var exportRead = ReadRequiredExports(installation, processArchitecture, paths);
        if (exportRead.Failure is not null)
        {
            return exportRead.Failure;
        }

        var exportBundle = exportRead.Exports!;
        var missingExports = RequiredEngineExports
            .Where(requiredExport => !exportBundle.Engine.Contains(requiredExport))
            .Concat(RequiredModuleExports.Where(requiredExport => !exportBundle.Module.Contains(requiredExport)))
            .Concat(RequiredCoreUtilsExports.Where(requiredExport => !exportBundle.CoreUtils.Contains(requiredExport)))
            .ToArray();
        var isReady = missingExports.Length == 0;
        var canCallFromCurrentProcess = isReady && RuntimeInformation.ProcessArchitecture == Architecture.X86;

        return new GnuCashNativeApiStatus(
            IsReady: isReady,
            CanCallFromCurrentProcess: canCallFromCurrentProcess,
            InstallPath: installation.InstallPath,
            DisplayVersion: installation.DisplayVersion,
            EnginePath: paths.Engine,
            ProcessArchitecture: processArchitecture,
            RequiredExports: GetRequiredExports(),
            MissingExports: missingExports,
            CheckedPaths: CreateCheckedPaths(installation, paths),
            Message: CreateMessage(isReady, canCallFromCurrentProcess, missingExports));
    }

    private RequiredExportReadResult ReadRequiredExports(
        GnuCashInstallationStatus installation,
        string processArchitecture,
        NativeApiProbePaths paths)
    {
        var engine = ReadLibraryExports(installation, processArchitecture, paths, paths.Engine, "libgnc-engine.dll");
        if (engine.Failure is not null)
        {
            return new RequiredExportReadResult(null, engine.Failure);
        }

        var module = ReadLibraryExports(installation, processArchitecture, paths, paths.Module, "libgnc-module.dll");
        if (module.Failure is not null)
        {
            return new RequiredExportReadResult(null, module.Failure);
        }

        var coreUtils = ReadLibraryExports(installation, processArchitecture, paths, paths.CoreUtils, "libgnc-core-utils.dll");
        return coreUtils.Failure is not null
            ? new RequiredExportReadResult(null, coreUtils.Failure)
            : new RequiredExportReadResult(
                new NativeApiExportBundle(engine.Exports!, module.Exports!, coreUtils.Exports!),
                null);
    }

    private LibraryExportReadResult ReadLibraryExports(
        GnuCashInstallationStatus installation,
        string processArchitecture,
        NativeApiProbePaths paths,
        string libraryPath,
        string libraryName)
    {
        try
        {
            return new LibraryExportReadResult(exportReader.ReadExportNames(libraryPath), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new LibraryExportReadResult(
                null,
                CreateInspectionFailureStatus(installation, processArchitecture, paths, libraryName, ex));
        }
    }

    private static GnuCashNativeApiStatus CreateInstallNotReadyStatus(
        GnuCashInstallationStatus installation,
        string processArchitecture)
    {
        var requiredExports = GetRequiredExports();
        return new GnuCashNativeApiStatus(
            IsReady: false,
            CanCallFromCurrentProcess: false,
            InstallPath: installation.InstallPath,
            DisplayVersion: installation.DisplayVersion,
            EnginePath: null,
            ProcessArchitecture: processArchitecture,
            RequiredExports: requiredExports,
            MissingExports: requiredExports,
            CheckedPaths: installation.CheckedPaths,
            Message: installation.Message);
    }

    private static GnuCashNativeApiStatus CreateInspectionFailureStatus(
        GnuCashInstallationStatus installation,
        string processArchitecture,
        NativeApiProbePaths paths,
        string libraryName,
        Exception exception) =>
        new(
            IsReady: false,
            CanCallFromCurrentProcess: false,
            InstallPath: installation.InstallPath,
            DisplayVersion: installation.DisplayVersion,
            EnginePath: paths.Engine,
            ProcessArchitecture: processArchitecture,
            RequiredExports: GetRequiredExports(),
            MissingExports: GetRequiredExports(),
            CheckedPaths: CreateCheckedPaths(installation, paths),
            Message: $"GnuCash is installed, but the bridge could not inspect {libraryName}: {exception.Message}");

    private static IReadOnlyList<string> CreateCheckedPaths(
        GnuCashInstallationStatus installation,
        NativeApiProbePaths paths) =>
        [.. installation.CheckedPaths, paths.Engine, paths.Module, paths.CoreUtils];

    private static IReadOnlyList<string> GetRequiredExports() =>
        [.. RequiredEngineExports, .. RequiredModuleExports, .. RequiredCoreUtilsExports];

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

    private sealed record NativeApiProbePaths(string Engine, string Module, string CoreUtils)
    {
        public static NativeApiProbePaths Create(string installPath) =>
            new(
                Path.Combine(installPath, "bin", "libgnc-engine.dll"),
                Path.Combine(installPath, "bin", "libgnc-module.dll"),
                Path.Combine(installPath, "bin", "libgnc-core-utils.dll"));
    }

    private sealed record NativeApiExportBundle(
        IReadOnlySet<string> Engine,
        IReadOnlySet<string> Module,
        IReadOnlySet<string> CoreUtils);

    private sealed record RequiredExportReadResult(
        NativeApiExportBundle? Exports,
        GnuCashNativeApiStatus? Failure);

    private sealed record LibraryExportReadResult(
        IReadOnlySet<string>? Exports,
        GnuCashNativeApiStatus? Failure);
}
