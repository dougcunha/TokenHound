using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Cursor;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cursor;

/// <summary>
/// Unit tests for <see cref="CursorUsageProvider"/>.
/// </summary>
public sealed class CursorUsageProviderTests
{
    [Fact]
    public async Task GetSnapshotAsync_WhenUnauthenticated_ReturnsNeedsAuthSnapshot()
    {
        // Arrange
        var discovery = new CursorSessionDiscovery("C:\\nonexistent_cursor_db_auth.vscdb");
        using var client = new CursorApiClient();
        using var provider = new CursorUsageProvider(discovery, client);

        // Act
        var snapshot = await provider.GetSnapshotAsync();

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal("cursor", snapshot.ProviderId);
        Assert.Equal(ProviderStatus.NeedsAuth, snapshot.Status);
        Assert.Equal(Fidelity.Official, snapshot.Fidelity);
        Assert.Empty(snapshot.LimitWindows);
        Assert.NotNull(snapshot.ErrorDescription);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithNormalUsage_ReturnsSnapshotWithWindows()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_usage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        CreateAuthDatabase(dbPath, "test-user-id", "test-token");

        const string JSON_PAYLOAD = """
            {
              "billingCycleEnd": "2026-09-24T00:00:00Z",
              "individualUsage": {
                "plan": {
                  "enabled": true,
                  "used": 42,
                  "limit": 100,
                  "totalPercentUsed": 42.5,
                  "apiPercentUsed": 10.0
                },
                "onDemand": {
                  "enabled": true,
                  "used": 5.0,
                  "limit": 20.0
                }
              }
            }
            """;

        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, JSON_PAYLOAD);
        using var httpClient = new HttpClient(handler);
        using var client = new CursorApiClient(httpClient);
        var discovery = new CursorSessionDiscovery(dbPath);
        using var provider = new CursorUsageProvider(discovery, client);

        try
        {
            // Act
            var snapshot = await provider.GetSnapshotAsync();

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("cursor", snapshot.ProviderId);
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Equal(3, snapshot.LimitWindows.Count);

            var planWindow = snapshot.LimitWindows[0];
            Assert.Equal("Plan allowance", planWindow.Name);
            Assert.Equal(0.425, planWindow.UsedFraction);
            Assert.Equal(100L, planWindow.TotalUnits);
            Assert.Equal(58L, planWindow.RemainingUnits);

            var apiWindow = snapshot.LimitWindows[1];
            Assert.Equal("API usage", apiWindow.Name);
            Assert.Equal(0.1, apiWindow.UsedFraction);

            var onDemandWindow = snapshot.LimitWindows[2];
            Assert.Equal("On-demand", onDemandWindow.Name);
            Assert.Equal(0.25, onDemandWindow.UsedFraction);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WithFreeTierBonusUsage_UsesTotalPercentUsedDirectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_free_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        CreateAuthDatabase(dbPath, "free-user-id", "free-token");

        const string JSON_PAYLOAD = """
            {
              "billingCycleEnd": "2026-09-24T00:00:00Z",
              "individualUsage": {
                "plan": {
                  "enabled": true,
                  "used": 0,
                  "limit": 0,
                  "totalPercentUsed": 9.5,
                  "apiPercentUsed": 0
                }
              }
            }
            """;

        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, JSON_PAYLOAD);
        using var httpClient = new HttpClient(handler);
        using var client = new CursorApiClient(httpClient);
        var discovery = new CursorSessionDiscovery(dbPath);
        using var provider = new CursorUsageProvider(discovery, client);

        try
        {
            // Act
            var snapshot = await provider.GetSnapshotAsync();

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(ProviderStatus.Ok, snapshot.Status);
            Assert.Single(snapshot.LimitWindows);

            var planWindow = snapshot.LimitWindows[0];
            Assert.Equal("Plan allowance", planWindow.Name);
            Assert.Equal(0.095, planWindow.UsedFraction);
            Assert.Equal(90L, planWindow.RemainingUnits);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenApiFails_ReturnsStaleSnapshot()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"cursor_fail_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "state.vscdb");

        CreateAuthDatabase(dbPath, "test-user-id", "test-token");

        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        using var httpClient = new HttpClient(handler);
        using var client = new CursorApiClient(httpClient);
        var discovery = new CursorSessionDiscovery(dbPath);
        using var provider = new CursorUsageProvider(discovery, client);

        try
        {
            // Act
            var snapshot = await provider.GetSnapshotAsync();

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(ProviderStatus.Stale, snapshot.Status);
            Assert.Empty(snapshot.LimitWindows);
            Assert.NotNull(snapshot.ErrorDescription);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void CreateAuthDatabase(string dbPath, string authId, string token)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE ItemTable (key TEXT PRIMARY KEY, value TEXT);";
        cmd.ExecuteNonQuery();

        InsertKey(connection, "cursorAuth/stripeMembershipAuthId", authId);
        InsertKey(connection, "cursorAuth/accessToken", token);
    }

    private static void InsertKey(SqliteConnection connection, string key, string value)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO ItemTable (key, value) VALUES (@k, @v);";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.ExecuteNonQuery();
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public TestHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
