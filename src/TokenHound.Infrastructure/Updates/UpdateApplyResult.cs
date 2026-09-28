namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// The immutable result of <see cref="UpdateApplyCoordinator.ApplyAsync"/>.
/// </summary>
public sealed record UpdateApplyResult
{
    /// <summary>Gets the result category.</summary>
    public required UpdateApplyStatus Status { get; init; }

    /// <summary>Gets a diagnostic explanation for failures, or <see langword="null"/>.</summary>
    public string? Detail { get; init; }
}
