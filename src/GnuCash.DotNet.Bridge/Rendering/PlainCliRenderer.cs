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

    private void WriteLogo()
    {
        writer.WriteLine(CliLogo.PlainBanner);
        writer.WriteLine();
    }
}
