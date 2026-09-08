namespace TokenHound.Core.Contracts;

/// <summary>
/// Marks an <see cref="IUsageProvider"/> that manages rate-limit deadlines at the individual request level.
/// </summary>
public interface IRequestGatedUsageProvider : IUsageProvider
{
}
