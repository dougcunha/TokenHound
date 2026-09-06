using AwesomeAssertions;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies Codex auth file discovery and JWT account claim decoding.
/// </summary>
public sealed class CodexAuthDiscoveryTests
{
    /// <summary>
    /// Verifies that a valid auth file yields the email and subscription plan.
    /// </summary>
    [Fact]
    public async Task DiscoverAsync_WithValidAuthFile_ReturnsAccountClaims()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var authDirectory = Path.Combine(tempDirectory, ".codex");
            Directory.CreateDirectory(authDirectory);
            var authPath = Path.Combine(authDirectory, "auth.json");
            await File.WriteAllTextAsync(authPath, CreateAuthJson("person@example.com", "plus"), TestContext.Current.CancellationToken);

            var discovery = new CodexAuthDiscovery(tempDirectory);
            var account = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

            account.Should().NotBeNull();
            account!.Email.Should().Be("person@example.com");
            account.PlanType.Should().Be("plus");
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>
    /// Verifies that a missing auth file returns null without throwing.
    /// </summary>
    [Fact]
    public async Task DiscoverAsync_WhenAuthFileIsMissing_ReturnsNull()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var discovery = new CodexAuthDiscovery(tempDirectory);
            var account = await discovery.DiscoverAsync(TestContext.Current.CancellationToken);

            account.Should().BeNull();
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>
    /// Verifies that malformed JWT values return null without throwing.
    /// </summary>
    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("header.payload")]
    [InlineData("header.%%%invalid%%%.signature")]
    [InlineData("header.eyJub3RfdGhlX3JpZ2h0X2NsYWltIjp0cnVlfQ.signature")]
    public void ParseAuthJson_WithMalformedToken_ReturnsNull(string token)
    {

        var authJson = JsonSerializer.Serialize(new { tokens = new { id_token = token } });

        var account = CodexAuthDiscovery.ParseAuthJson(authJson);

        account.Should().BeNull();
    }

    /// <summary>
    /// Verifies that malformed auth JSON returns null without throwing.
    /// </summary>
    [Fact]
    public void ParseAuthJson_WithMalformedJson_ReturnsNull()
    {

        var account = CodexAuthDiscovery.ParseAuthJson("{ invalid json");

        account.Should().BeNull();
    }

    private static string CreateAuthJson(string email, string planType)
    {

        var payloadWithAuth = $"{{\"email\":{JsonSerializer.Serialize(email)},\"https://api.openai.com/auth\":{{\"chatgpt_plan_type\":{JsonSerializer.Serialize(planType)}}}}}";
        var header = EncodeBase64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var encodedPayload = EncodeBase64Url(payloadWithAuth);

        return JsonSerializer.Serialize(new
        {
            tokens = new { id_token = $"{header}.{encodedPayload}.signature" }
        });
    }

    private static string EncodeBase64Url(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string CreateTempDirectory()
    {

        var path = Path.Combine(Path.GetTempPath(), $"codex_auth_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteTempDirectory(string path)
    {

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
