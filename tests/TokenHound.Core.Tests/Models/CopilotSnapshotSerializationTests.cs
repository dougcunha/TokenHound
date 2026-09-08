using AwesomeAssertions;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Core.Tests.Models;

/// <summary>
/// Verifies backward-compatible serialization of the optional Copilot billing state.
/// </summary>
public sealed class CopilotSnapshotSerializationTests
{
    /// <summary>
    /// Verifies old snapshots deserialize without billing while retaining quota data.
    /// </summary>
    [Fact]
    public void Snapshot_OldJsonWithoutBilling_DeserializesWithNullBilling()
    {
        const string json =
            """
            {
              "ProviderId": "copilot",
              "Status": 0,
              "Fidelity": 0,
              "FetchedAtUtc": "2026-09-01T00:00:00+00:00",
              "LimitWindows": [
                {
                  "Name": "Premium",
                  "RemainingUnits": 5,
                  "TotalUnits": 10,
                  "UsedFraction": 0.5
                }
              ]
            }
            """;

        var snapshot = JsonSerializer.Deserialize<Snapshot>(json);

        snapshot.Should().NotBeNull();
        snapshot!.CopilotBilling.Should().BeNull();
        snapshot.LimitWindows[0].RemainingUnits.Should().Be(5);
        snapshot.LimitWindows[0].TotalUnits.Should().Be(10);
    }
}
