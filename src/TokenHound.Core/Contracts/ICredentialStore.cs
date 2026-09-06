using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Core.Contracts;

/// <summary>
/// Provides secure, read-only access to stored credentials or authentication tokens.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Reads a stored credential for the specified target key or service identifier.
    /// </summary>
    /// <param name="target">The target key, service name, or resource identifier.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{String}"/> containing the credential string,
    /// or <see langword="null"/> if the credential does not exist.
    /// </returns>
    ValueTask<string?> ReadCredentialAsync(string target, CancellationToken cancellationToken = default);
}
