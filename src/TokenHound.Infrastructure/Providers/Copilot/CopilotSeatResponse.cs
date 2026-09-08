using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents seat assignments from the Copilot organization billing seats endpoint.
/// </summary>
public sealed record CopilotSeatResponse
{
    /// <summary>
    /// Gets the total seat count for the organization.
    /// </summary>
    [JsonPropertyName("total_seats")]
    public int? TotalSeats { get; init; }

    /// <summary>
    /// Gets the list of seat assignments.
    /// </summary>
    [JsonPropertyName("seats")]
    public IReadOnlyList<SeatAssignment>? Seats { get; init; }

    /// <summary>
    /// Represents one seat assignment record.
    /// </summary>
    public sealed record SeatAssignment
    {
        /// <summary>
        /// Gets the assignee information.
        /// </summary>
        [JsonPropertyName("assignee")]
        public AssigneeDto? Assignee { get; init; }

        /// <summary>
        /// Gets the plan type assigned to this seat.
        /// </summary>
        [JsonPropertyName("plan_type")]
        public string? PlanType { get; init; }

        /// <summary>
        /// Gets the creation timestamp of the seat assignment.
        /// </summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        /// <summary>
        /// Gets the pending cancellation date, if any.
        /// </summary>
        [JsonPropertyName("pending_cancellation_date")]
        public string? PendingCancellationDate { get; init; }
    }

    /// <summary>
    /// Represents the user assigned to a Copilot seat.
    /// </summary>
    public sealed record AssigneeDto
    {
        /// <summary>
        /// Gets the user login.
        /// </summary>
        [JsonPropertyName("login")]
        public string? Login { get; init; }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public long? Id { get; init; }
    }
}
