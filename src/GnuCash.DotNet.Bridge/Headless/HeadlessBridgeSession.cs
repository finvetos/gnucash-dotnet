using System.Text.Json;
using GnuCash.DotNet.Protocol.Contracts;

namespace GnuCash.DotNet.Bridge.Headless;

/// <summary>
/// Runs the machine protocol used by the SDK over newline-delimited JSON.
/// </summary>
public sealed class HeadlessBridgeSession
{
    private readonly BridgeRequestProcessor processor;

    public HeadlessBridgeSession(BridgeRequestProcessor processor)
    {
        this.processor = processor;
    }

    public async Task<int> RunAsync(
        TextReader input,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        string? line;
        while ((line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var shouldStop = false;
            var response = ProcessLine(line, error, out shouldStop);
            await WriteResponseAsync(output, response, cancellationToken).ConfigureAwait(false);

            if (shouldStop)
            {
                return response.Succeeded ? 0 : 2;
            }
        }

        return 0;
    }

    private BridgeResponse ProcessLine(string line, TextWriter error, out bool shouldStop)
    {
        shouldStop = false;

        BridgeRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(line, BridgeJson.SerializerOptions);
        }
        catch (JsonException ex)
        {
            error.WriteLine($"Malformed bridge request: {ex.Message}");
            return MalformedRequest();
        }

        if (request is null)
        {
            error.WriteLine("Malformed bridge request: request was empty.");
            return MalformedRequest();
        }

        shouldStop = request.Kind == BridgeRequestKind.Shutdown;

        try
        {
            return processor.Process(request);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            error.WriteLine($"Bridge request failed: {ex.Message}");
            return new BridgeResponse(
                request.Id,
                false,
                ErrorCode: "BridgeFailure",
                ErrorMessage: ex.Message);
        }
    }

    private static BridgeResponse MalformedRequest() =>
        new(
            Guid.Empty,
            false,
            ErrorCode: "MalformedRequest",
            ErrorMessage: "Input line was not a valid bridge request.");

    private static async Task WriteResponseAsync(
        TextWriter output,
        BridgeResponse response,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(response, BridgeJson.SerializerOptions);
        await output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
