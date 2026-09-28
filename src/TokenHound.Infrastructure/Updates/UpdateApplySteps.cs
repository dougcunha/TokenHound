using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// The environment facts and side effects <see cref="UpdateApplyCoordinator"/> sequences; the App supplies the
/// real downloader, applier, and launcher, tests supply fakes.
/// </summary>
public sealed record UpdateApplySteps
{
    /// <summary>Gets how the running copy was installed.</summary>
    public required InstallMode Mode { get; init; }

    /// <summary>Gets the architecture of the running process.</summary>
    public required Architecture Architecture { get; init; }

    /// <summary>Gets the probe that tells whether the application folder accepts writes.</summary>
    public required Func<bool> IsWritable { get; init; }

    /// <summary>Gets the download-and-verify step.</summary>
    public required Func<ReleaseAsset, IProgress<double>?, CancellationToken, Task<DownloadedUpdate>> DownloadAsync { get; init; }

    /// <summary>Gets the step that swaps the portable files and starts the new executable, given the verified zip path.</summary>
    public required Action<string> ApplyPortable { get; init; }

    /// <summary>Gets the step that launches the silent installer, given the verified setup path.</summary>
    public required Action<string> LaunchInstaller { get; init; }
}
