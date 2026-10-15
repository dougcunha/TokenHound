using System.Text.Json;

namespace TokenHound.Infrastructure.Providers.Codex;

internal static class CodexServerProtocol
{
    private const string INITIALIZE = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"TokenHound\",\"title\":\"TokenHound\",\"version\":\"1.0\"}}}";
    private const string INITIALIZED = "{\"jsonrpc\":\"2.0\",\"method\":\"initialized\",\"params\":{}}";
    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { PropertyNameCaseInsensitive = true };

    internal static async Task InitializeAsync(CodexAppServerClient.ICodexProcess process, CancellationToken cancellationToken)
    {

        await SendAsync(process.StandardInput, INITIALIZE, cancellationToken).ConfigureAwait(false);
        var response = await ReadResponseAsync(process.StandardOutput, 1, cancellationToken).ConfigureAwait(false);

        if (response.TryGetProperty("error", out _))
            throw new IOException("Codex app-server initialization was rejected.");

        await SendAsync(process.StandardInput, INITIALIZED, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<CodexRateLimitsDto?> ReadLimitsAsync(
        CodexAppServerClient.ICodexProcess process,
        int requestId,
        CancellationToken cancellationToken)
    {

        var request = $"{{\"jsonrpc\":\"2.0\",\"id\":{requestId},\"method\":\"account/rateLimits/read\",\"params\":null}}";
        await SendAsync(process.StandardInput, request, cancellationToken).ConfigureAwait(false);
        var response = await ReadResponseAsync(process.StandardOutput, requestId, cancellationToken).ConfigureAwait(false);

        return response.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Object
            && result.TryGetProperty("rateLimits", out var limits)
                ? limits.Deserialize<CodexRateLimitsDto>(SERIALIZER_OPTIONS)
                : null;
    }

    private static async Task SendAsync(TextWriter writer, string request, CancellationToken cancellationToken)
    {

        await writer.WriteLineAsync(request.AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonElement> ReadResponseAsync(TextReader reader, int id, CancellationToken cancellationToken)
    {

        while (true)
        {

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line is null)
                throw new IOException("Codex app-server closed its output stream.");

            if (TryMatch(line, id) is { } response)
                return response;
        }
    }

    private static JsonElement? TryMatch(string line, int id)
    {

        try
        {

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("id", out var responseId)
                && responseId.ValueKind == JsonValueKind.Number
                && responseId.TryGetInt32(out var value)
                && value == id ? root.Clone() : null;
        }
        catch (JsonException)
        {

            return null;
        }
    }
}
