using System.Diagnostics;
using System.Text.Json;
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Protocol.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GnuCash.DotNet.Services;

internal sealed class GnuCashBridgeProcess
{
    private const string BridgePathEnvironmentVariable = "GNUCASH_DOTNET_BRIDGE_PATH";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<GnuCashBridgeProcess> logger;
    private readonly GnuCashBridgeOptions options;

    public GnuCashBridgeProcess(
        ILogger<GnuCashBridgeProcess> logger,
        IOptions<GnuCashBridgeOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task<BridgeResponse> SendAsync(
        BridgeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var launch = ResolveLaunchInfo();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.StartupTimeoutSeconds));

        using var process = new Process
        {
            StartInfo = CreateStartInfo(launch)
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the GnuCash bridge process.");
        }

        logger.LogDebug("Started GnuCash bridge process {ProcessId}.", process.Id);

        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
        var requestJson = JsonSerializer.Serialize(request, SerializerOptions);

        await process.StandardInput.WriteLineAsync(requestJson.AsMemory(), timeout.Token).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(timeout.Token).ConfigureAwait(false);
        process.StandardInput.Close();

        var responseLine = await process.StandardOutput.ReadLineAsync(timeout.Token).ConfigureAwait(false);
        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);

        var stderr = await ReadStandardErrorAsync(stderrTask).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new InvalidOperationException(
                $"The GnuCash bridge did not return a protocol response. {stderr}".Trim());
        }

        var response = JsonSerializer.Deserialize<BridgeResponse>(responseLine, SerializerOptions);
        if (response is null)
        {
            throw new InvalidOperationException("The GnuCash bridge returned an empty protocol response.");
        }

        if (process.ExitCode != 0 && !response.Succeeded)
        {
            logger.LogWarning(
                "GnuCash bridge exited with code {ExitCode}: {Error}",
                process.ExitCode,
                stderr);
        }

        return response;
    }

    private ProcessStartInfo CreateStartInfo(BridgeLaunchInfo launch)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.FileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in launch.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("headless");
        startInfo.ArgumentList.Add("--stdio");

        if (!string.IsNullOrWhiteSpace(options.InstallPath))
        {
            startInfo.Environment["GNUCASH_HOME"] = options.InstallPath;
        }

        return startInfo;
    }

    private BridgeLaunchInfo ResolveLaunchInfo()
    {
        var bridgePath = ResolveBridgePath();
        if (string.Equals(Path.GetExtension(bridgePath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            return new BridgeLaunchInfo("dotnet", [bridgePath]);
        }

        return new BridgeLaunchInfo(bridgePath, []);
    }

    private string ResolveBridgePath()
    {
        if (!string.IsNullOrWhiteSpace(options.BridgeExecutablePath))
        {
            return ResolveRequiredBridgePath(
                options.BridgeExecutablePath,
                "Configured GnuCash:BridgeExecutablePath does not exist.");
        }

        var environmentPath = Environment.GetEnvironmentVariable(BridgePathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentPath))
        {
            return ResolveRequiredBridgePath(
                environmentPath,
                $"Configured {BridgePathEnvironmentVariable} does not exist.");
        }

        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "GnuCash.DotNet.Bridge.exe"),
            Path.Combine(baseDirectory, "GnuCash.DotNet.Bridge.dll"),
            Path.Combine(baseDirectory, "bridge-win-x86", "GnuCash.DotNet.Bridge.exe"),
            Path.Combine(baseDirectory, "bridge-win-x86", "GnuCash.DotNet.Bridge.dll")
        };

        var configuredPath = FirstExistingPath(candidates);
        if (configuredPath is not null)
        {
            return configuredPath;
        }

        throw new FileNotFoundException(
            "GnuCash.DotNet could not find the bridge executable. Configure GnuCash:BridgeExecutablePath or set GNUCASH_DOTNET_BRIDGE_PATH.");
    }

    private static string? FirstExistingPath(params string?[] candidates) =>
        candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => Path.GetFullPath(candidate!))
            .FirstOrDefault(path => File.Exists(path));

    private static string ResolveRequiredBridgePath(string path, string errorMessage)
    {
        var fullPath = Path.GetFullPath(path);
        return File.Exists(fullPath) ? fullPath : throw new FileNotFoundException(errorMessage, fullPath);
    }

    private static async Task<string> ReadStandardErrorAsync(Task<string> stderrTask)
    {
        try
        {
            return await stderrTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private sealed record BridgeLaunchInfo(string FileName, IReadOnlyList<string> Arguments);
}
