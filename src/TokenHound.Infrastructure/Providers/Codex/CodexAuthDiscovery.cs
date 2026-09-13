using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>
/// Discovers the Codex account identity from the local auth file and JWT payload.
/// </summary>
public sealed class CodexAuthDiscovery
{
    private const string AUTH_DIRECTORY_NAME = ".codex";
    private const string AUTH_FILE_NAME = "auth.json";
    private const string AUTH_CLAIMS_PROPERTY_NAME = "https://api.openai.com/auth";
    private const string EMAIL_PROPERTY_NAME = "email";
    private const string ID_TOKEN_PROPERTY_NAME = "id_token";
    private const string PLAN_TYPE_PROPERTY_NAME = "chatgpt_plan_type";
    private const string TOKENS_PROPERTY_NAME = "tokens";

    private readonly string _baseDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexAuthDiscovery"/> class.
    /// </summary>
    /// <param name="baseDirectory">
    /// The user profile directory containing the <c>.codex</c> directory, or
    /// <see langword="null"/> to use the current user's profile directory.
    /// </param>
    public CodexAuthDiscovery(string? baseDirectory = null)
    {

        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : baseDirectory;
    }

    /// <summary>
    /// Gets the user profile directory used for auth discovery.
    /// </summary>
    public string BaseDirectory
        => _baseDirectory;

    /// <summary>
    /// Gets the path of the Codex auth file used for discovery.
    /// </summary>
    public string AuthFilePath
        => Path.Combine(_baseDirectory, AUTH_DIRECTORY_NAME, AUTH_FILE_NAME);

    /// <summary>
    /// Reads the local Codex auth file and extracts its account identity.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The discovered account identity, or <see langword="null"/> if unavailable.</returns>
    public Task<AccountInfo?> DiscoverAsync(CancellationToken cancellationToken = default)
        => LoadFromFileAsync(AuthFilePath, cancellationToken);

    /// <summary>
    /// Reads and parses a Codex auth file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the Codex auth file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The discovered account identity, or <see langword="null"/> if unavailable.</returns>
    public static async Task<AccountInfo?> LoadFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {

            var jsonContent = await SharedFileReader.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);

            return ParseAuthJson(jsonContent);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            return null;
        }
    }

    /// <summary>
    /// Parses Codex auth JSON and decodes the payload of its ID token.
    /// </summary>
    /// <param name="jsonContent">The raw auth JSON.</param>
    /// <returns>The discovered account identity, or <see langword="null"/> if the content is invalid.</returns>
    public static AccountInfo? ParseAuthJson(string? jsonContent)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(TOKENS_PROPERTY_NAME, out var tokens) ||
                tokens.ValueKind != JsonValueKind.Object ||
                !tokens.TryGetProperty(ID_TOKEN_PROPERTY_NAME, out var idToken) ||
                idToken.ValueKind != JsonValueKind.String)
                return null;

            return ParseJwtPayload(idToken.GetString());
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private static AccountInfo? ParseJwtPayload(string? token)
    {

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var segments = token.Split('.');

        if (segments.Length != 3)
            return null;

        try
        {

            var payloadJson = DecodeBase64Url(segments[1]);

            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var email = TryGetStringProperty(root, EMAIL_PROPERTY_NAME);
            var planType = TryGetPlanType(root);

            if (email is null && planType is null)
                return null;

            return new AccountInfo { Email = email, PlanType = planType };
        }
        catch (FormatException)
        {

            return null;
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private static string DecodeBase64Url(string value)
    {

        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = base64.Length % 4;

        if (padding > 0)
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string? TryGetPlanType(JsonElement root)
    {

        if (!root.TryGetProperty(AUTH_CLAIMS_PROPERTY_NAME, out var authClaims) ||
            authClaims.ValueKind != JsonValueKind.Object)
            return null;

        return TryGetStringProperty(authClaims, PLAN_TYPE_PROPERTY_NAME);
    }

    private static string? TryGetStringProperty(JsonElement element, string propertyName)
    {

        return element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    /// <summary>
    /// Represents the account identity claims discovered from the Codex ID token.
    /// </summary>
    public sealed record AccountInfo
    {
        /// <summary>
        /// Gets the OpenAI account email address, if present in the token.
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// Gets the ChatGPT subscription plan type, if present in the token.
        /// </summary>
        public string? PlanType { get; init; }

        /// <summary>
        /// Gets the ChatGPT subscription plan type, if present in the token.
        /// </summary>
        public string? ChatGptPlanType
            => PlanType;
    }
}
