using System.Text.Json;
using GnuCash.DotNet.Bridge.Commands;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Rendering;

public sealed class PlainCliRenderer : ICliRenderer
{
    private readonly TextWriter writer;
    private readonly bool json;

    public PlainCliRenderer(TextWriter writer, bool json = false)
    {
        this.writer = writer;
        this.json = json;
    }

    public void WriteHelp(IReadOnlyList<CommandDescriptor> commands)
    {
        if (json)
        {
            WriteJson(new { name = "GnuCash.DotNet.Bridge", commands });
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash.DotNet.Bridge");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  GnuCash.DotNet.Bridge <command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        foreach (var command in commands)
        {
            writer.WriteLine($"  {command.Name,-16} {command.Description}");
        }

        writer.WriteLine();
        writer.WriteLine("Output:");
        writer.WriteLine("  --plain          Use plain ASCII output.");
        writer.WriteLine("  --json           Emit JSON where supported.");
        writer.WriteLine("  --format <mode>  auto, rich, plain, or json.");
    }

    public void WriteCommandList(IReadOnlyList<CommandDescriptor> commands)
    {
        if (json)
        {
            WriteJson(new { commands });
            return;
        }

        WriteLogo();
        writer.WriteLine("Commands");
        writer.WriteLine("--------");
        foreach (var command in commands)
        {
            writer.WriteLine($"{command.Name,-16} {command.Description}");
            writer.WriteLine($"                 {command.Usage}");
        }
    }

    public void WriteGnuCashValidation(GnuCashInstallationStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash installation validation");
        writer.WriteLine("-------------------------------");
        writer.WriteLine($"Status:  {(status.IsReady ? "Ready" : "Not ready")}");

        if (status.IsReady)
        {
            writer.WriteLine($"Path:    {status.InstallPath}");
            writer.WriteLine($"Version: {status.DisplayVersion ?? "unknown"}");
            writer.WriteLine($"Source:  {status.Source ?? "unknown"}");
            writer.WriteLine();
            writer.WriteLine("GnuCash is installed and the bridge can use it.");
            return;
        }

        writer.WriteLine();
        writer.WriteLine(status.Message);
        WriteList("Checked paths", status.CheckedPaths);
        WriteList("Missing required paths", status.MissingPaths);
    }

    public void WriteNativeApiValidation(GnuCashNativeApiStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash native API validation");
        writer.WriteLine("-----------------------------");
        writer.WriteLine($"Status:       {(status.IsReady ? "Ready" : "Not ready")}");
        writer.WriteLine($"Callable now: {(status.CanCallFromCurrentProcess ? "Yes" : "No")}");
        writer.WriteLine($"Architecture: {status.ProcessArchitecture}");

        if (!string.IsNullOrWhiteSpace(status.InstallPath))
        {
            writer.WriteLine($"Path:         {status.InstallPath}");
        }

        if (!string.IsNullOrWhiteSpace(status.EnginePath))
        {
            writer.WriteLine($"Engine:       {status.EnginePath}");
        }

        if (!string.IsNullOrWhiteSpace(status.DisplayVersion))
        {
            writer.WriteLine($"Version:      {status.DisplayVersion}");
        }

        writer.WriteLine();
        writer.WriteLine(status.Message);
        WriteList("Missing native exports", status.MissingExports);
    }

    public void WriteNativeExportInventory(GnuCashNativeExportInventoryStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash native export inventory");
        writer.WriteLine("-------------------------------");
        writer.WriteLine($"Status:    {(status.IsReady ? "Ready" : "Not ready")}");
        writer.WriteLine($"Libraries: {status.LibraryCount}");
        writer.WriteLine($"Exports:   {status.TotalExportCount}");
        WriteOptional("Path", status.InstallPath);
        WriteOptional("Version", status.DisplayVersion);
        writer.WriteLine();
        writer.WriteLine(status.Message);

        foreach (var library in status.Libraries)
        {
            writer.WriteLine();
            writer.WriteLine($"{library.Name}: {(library.IsReady ? "Ready" : "Not ready")} ({library.ExportCount} exports)");
            WriteOptional("Path", library.Path);
            WriteOptional("Error", library.ErrorMessage);
            WriteList("Sample exports", library.Exports.Take(20).ToArray());
            if (library.Exports.Count > 20)
            {
                writer.WriteLine($"  ... {library.Exports.Count - 20} more exports; use --json for the full inventory.");
            }
        }
    }

    public void WriteNativeSessionValidation(GnuCashNativeSessionStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash native session validation");
        writer.WriteLine("---------------------------------");
        writer.WriteLine($"Status:       {(status.IsReady ? "Ready" : "Not ready")}");
        writer.WriteLine($"Callable now: {(status.CanCallFromCurrentProcess ? "Yes" : "No")}");
        writer.WriteLine($"Architecture: {status.ProcessArchitecture}");
        writer.WriteLine($"Book:         {status.BookPath}");

        if (!string.IsNullOrWhiteSpace(status.InstallPath))
        {
            writer.WriteLine($"Path:         {status.InstallPath}");
        }

        if (!string.IsNullOrWhiteSpace(status.EnginePath))
        {
            writer.WriteLine($"Engine:       {status.EnginePath}");
        }

        if (!string.IsNullOrWhiteSpace(status.DisplayVersion))
        {
            writer.WriteLine($"Version:      {status.DisplayVersion}");
        }

        if (!string.IsNullOrWhiteSpace(status.SessionFilePath))
        {
            writer.WriteLine($"Session file: {status.SessionFilePath}");
        }

        if (!string.IsNullOrWhiteSpace(status.SessionUrl))
        {
            writer.WriteLine($"Session URL:  {status.SessionUrl}");
        }

        writer.WriteLine($"Has book:     {(status.HasBook ? "Yes" : "No")}");
        writer.WriteLine($"Has root:     {(status.HasRootAccount ? "Yes" : "No")}");

        if (status.AccountCount is not null)
        {
            writer.WriteLine($"Accounts:     {status.AccountCount}");
        }

        if (status.CommodityCount is not null)
        {
            writer.WriteLine($"Commodities:  {status.CommodityCount}");
        }

        if (status.TransactionCount is not null)
        {
            writer.WriteLine($"Transactions: {status.TransactionCount}");
        }

        if (status.BackendErrorCode is not null)
        {
            writer.WriteLine($"Backend err:  {status.BackendErrorCode}");
        }

        if (!string.IsNullOrWhiteSpace(status.BackendErrorMessage))
        {
            writer.WriteLine($"Backend msg:  {status.BackendErrorMessage}");
        }

        writer.WriteLine();
        writer.WriteLine(status.Message);
    }

    public void WriteNativeReadParityValidation(GnuCashNativeReadParityStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash native read parity validation");
        writer.WriteLine("-------------------------------------");
        writer.WriteLine($"Status:       {(status.IsReady ? "Ready" : "Not ready")}");
        writer.WriteLine($"Callable now: {(status.CanCallFromCurrentProcess ? "Yes" : "No")}");
        writer.WriteLine($"Architecture: {status.ProcessArchitecture}");
        writer.WriteLine($"Book:         {status.BookPath}");
        WriteOptional("Path", status.InstallPath);
        WriteOptional("Engine", status.EnginePath);
        WriteOptional("Version", status.DisplayVersion);
        WriteOptional("Native book", status.NativeBookId);
        WriteOptional("XML book", status.XmlBookId);
        WriteOptional("Native acct", status.NativeAccountCount?.ToString());
        WriteOptional("XML acct", status.XmlAccountCount?.ToString());
        WriteOptional("Native cmdty", status.NativeCommodityCount?.ToString());
        WriteOptional("XML cmdty", status.XmlCommodityCount?.ToString());
        WriteOptional("Native txns", status.NativeTransactionCount?.ToString());
        WriteOptional("XML txns", status.XmlTransactionCount?.ToString());
        WriteOptional("Native splits", status.NativeSplitCount?.ToString());
        WriteOptional("XML splits", status.XmlSplitCount?.ToString());
        WriteOptional("Native prices", status.NativePriceCount?.ToString());
        WriteOptional("XML prices", status.XmlPriceCount?.ToString());
        writer.WriteLine();
        writer.WriteLine(status.Message);
        WriteList("Book mismatches", status.BookMismatches);
        WriteList("Account mismatches", status.AccountMismatches);
        WriteList("Commodity mismatches", status.CommodityMismatches);
        WriteList("Transaction mismatches", status.TransactionMismatches);
        WriteList("Price mismatches", status.PriceMismatches);
    }

    public void WriteNativeWriteRoundTripValidation(GnuCashNativeWriteRoundTripStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash native write round-trip validation");
        writer.WriteLine("------------------------------------------");
        writer.WriteLine($"Status:       {(status.IsReady ? "Ready" : "Not ready")}");
        writer.WriteLine($"Callable now: {(status.CanCallFromCurrentProcess ? "Yes" : "No")}");
        writer.WriteLine($"Architecture: {status.ProcessArchitecture}");
        writer.WriteLine($"Source:       {status.SourceBookPath}");
        writer.WriteLine($"Working:      {status.WorkingBookPath}");
        WriteOptional("Path", status.InstallPath);
        WriteOptional("Engine", status.EnginePath);
        WriteOptional("Version", status.DisplayVersion);
        WriteOptional("Before acct", status.BeforeAccountCount?.ToString());
        WriteOptional("After acct", status.AfterAccountCount?.ToString());
        WriteOptional("Before cmdty", status.BeforeCommodityCount?.ToString());
        WriteOptional("After cmdty", status.AfterCommodityCount?.ToString());
        WriteOptional("Before txns", status.BeforeTransactionCount?.ToString());
        WriteOptional("After txns", status.AfterTransactionCount?.ToString());
        WriteOptional("Backend err", status.BackendErrorCode?.ToString());
        WriteOptional("Backend msg", status.BackendErrorMessage);
        writer.WriteLine();
        writer.WriteLine(status.Message);
    }

    public void WriteNativeCustomerWriteValidation(GnuCashNativeCustomerWriteStatus status)
    {
        if (json)
        {
            WriteJson(status);
            return;
        }

        WriteLogo();
        writer.WriteLine("GnuCash native customer write validation");
        writer.WriteLine("----------------------------------------");
        writer.WriteLine($"Status:       {(status.IsReady ? "Ready" : "Not ready")}");
        writer.WriteLine($"Callable now: {(status.CanCallFromCurrentProcess ? "Yes" : "No")}");
        writer.WriteLine($"Architecture: {status.ProcessArchitecture}");
        writer.WriteLine($"Source:       {status.SourceBookPath}");
        writer.WriteLine($"Working:      {status.WorkingBookPath}");
        writer.WriteLine($"Customer:     {status.CustomerId} - {status.CustomerName}");
        writer.WriteLine($"Currency:     {status.CurrencySpace}::{status.CurrencyId}");
        WriteOptional("Guid", status.CreatedCustomerGuid);
        WriteOptional("Before cust", status.BeforeCustomerCount?.ToString());
        WriteOptional("After cust", status.AfterCustomerCount?.ToString());
        WriteOptional("Found", status.FoundAfterReopen ? "Yes" : "No");
        WriteOptional("Backend err", status.BackendErrorCode?.ToString());
        WriteOptional("Backend msg", status.BackendErrorMessage);
        writer.WriteLine();
        writer.WriteLine(status.Message);
    }

    public void WriteJson<T>(T value)
    {
        writer.WriteLine(JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private void WriteList(string title, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine(title + ":");
        foreach (var value in values)
        {
            writer.WriteLine($"  {value}");
        }
    }

    private void WriteOptional(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteLine($"{name + ":",-13} {value}");
        }
    }

    private void WriteLogo()
    {
        writer.WriteLine(CliLogo.PlainBanner);
        writer.WriteLine();
    }
}
