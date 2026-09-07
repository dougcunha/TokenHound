using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Reads Copilot CLI JSONC state with shared, read-only file access.
/// </summary>
public sealed partial class CopilotConfigReader
{
    /// <summary>The managed Copilot CLI configuration file name.</summary>
    public const string CONFIG_FILE_NAME = "config.json";

    private readonly string _baseDirectory;
    private readonly string? _verifiedTokenPropertyPath;

    /// <summary>
    /// Initializes a reader for a Copilot home directory.
    /// </summary>
    /// <param name="baseDirectory">The Copilot home directory, or null to use the documented default.</param>
    /// <param name="verifiedTokenPropertyPath">
    /// An exact JSON property path verified against an official fixture; null disables the fallback.
    /// </param>
    public CopilotConfigReader(
        string? baseDirectory = null,
        string? verifiedTokenPropertyPath = null)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? ResolveDefaultDirectory()
            : baseDirectory;
        _verifiedTokenPropertyPath = verifiedTokenPropertyPath;
    }

    /// <summary>Gets the Copilot home directory used by this reader.</summary>
    public string BaseDirectory
        => _baseDirectory;

    /// <summary>Gets the config file path used by this reader.</summary>
    public string ConfigFilePath
        => Path.Combine(_baseDirectory, CONFIG_FILE_NAME);

    /// <summary>Gets whether a verified plaintext token property was explicitly configured.</summary>
    public bool IsPlaintextTokenFallbackEnabled
        => !string.IsNullOrWhiteSpace(_verifiedTokenPropertyPath);

    /// <summary>Reads the allowlisted plaintext token without modifying the config file.</summary>
    /// <param name="cancellationToken">A token to observe while reading.</param>
    /// <returns>The token, or null when the branch is unavailable or invalid.</returns>
    public async ValueTask<string?> ReadTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPlaintextTokenFallbackEnabled)
            return null;

        var jsonContent = await ReadConfigAsync(cancellationToken).ConfigureAwait(false);

        return ParseToken(jsonContent, _verifiedTokenPropertyPath);
    }

    /// <summary>Reads validated login state used to derive Copilot keychain targets.</summary>
    /// <param name="cancellationToken">A token to observe while reading.</param>
    /// <returns>The login state, or null when the state is unavailable or malformed.</returns>
    public async ValueTask<LoginState?> ReadLoginStateAsync(
        CancellationToken cancellationToken = default)
    {
        var jsonContent = await ReadConfigAsync(cancellationToken).ConfigureAwait(false);

        return ParseLoginState(jsonContent);
    }

    /// <summary>
    /// Parses an exact allowlisted property from JSONC content.
    /// </summary>
    /// <param name="jsonContent">The JSONC content to parse.</param>
    /// <param name="verifiedTokenPropertyPath">An exact property name or slash-separated path.</param>
    /// <returns>The trimmed token, or null when parsing or validation fails.</returns>
    public static string? ParseToken(
        string? jsonContent,
        string? verifiedTokenPropertyPath)
    {
        if (string.IsNullOrWhiteSpace(jsonContent)
            || string.IsNullOrWhiteSpace(verifiedTokenPropertyPath))
            return null;

        try
        {
            using var document = ParseJsonc(jsonContent);
            var property = FindProperty(document.RootElement, verifiedTokenPropertyPath);

            return property?.ValueKind == JsonValueKind.String
                ? TrimUsable(property.Value.GetString())
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Parses Copilot login state from JSONC content.</summary>
    /// <param name="jsonContent">The JSONC content to parse.</param>
    /// <returns>Validated state, or null when the document is malformed.</returns>
    public static LoginState? ParseLoginState(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {
            using var document = ParseJsonc(jsonContent);
            var root = document.RootElement;
            var last = TryReadIdentity(root, "lastLoggedInUser");
            var loggedInUsers = ReadLoggedInUsers(root);

            return new LoginState
            {
                LastLoggedInUser = last,
                LoggedInUsers = loggedInUsers
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async ValueTask<string?> ReadConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await SharedFileReader.ReadAllTextAsync(
                ConfigFilePath,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static JsonDocument ParseJsonc(string content)
        => JsonDocument.Parse(
            content,
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

    private static JsonElement? FindProperty(
        JsonElement root,
        string propertyPath)
    {
        var current = root;
        var segments = propertyPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return null;

        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current;
    }

    private static LoginIdentity? TryReadIdentity(
        JsonElement root,
        string propertyName)
    {
        return TryGetPropertyIgnoreCase(root, propertyName, out var element)
            ? TryReadIdentity(element)
            : null;
    }

    private static LoginIdentity? TryReadIdentity(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !TryGetString(element, "host", out var host)
            || !TryGetString(element, "login", out var login))
            return null;

        return new LoginIdentity { Host = host, Login = login };
    }

    private static IReadOnlyList<LoginIdentity> ReadLoggedInUsers(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "loggedInUsers", out var users))
            return [];

        var identities = new List<LoginIdentity>();

        if (users.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in users.EnumerateArray())
                AddIdentity(identities, TryReadIdentity(item));
        }
        else if (users.ValueKind == JsonValueKind.Object)
        {
            var direct = TryReadIdentity(users);

            if (direct is not null)
                identities.Add(direct);
            else
            {
                foreach (var item in users.EnumerateObject())
                    AddIdentity(identities, TryReadIdentity(item.Value));
            }
        }

        return identities;
    }

    private static void AddIdentity(
        ICollection<LoginIdentity> identities,
        LoginIdentity? identity)
    {
        if (identity is not null)
            identities.Add(identity);
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string value)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(property.GetString()))
        {
            value = property.GetString()!.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? TrimUsable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ResolveDefaultDirectory()
        => Environment.GetEnvironmentVariable("COPILOT_HOME")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".copilot");

}
