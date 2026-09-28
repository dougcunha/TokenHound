using Serilog;
using System;
using System.Linq;
using TokenHound.Infrastructure.System;
using TokenHound.Infrastructure.Updates;

namespace TokenHound.App;

/// <summary>Holds the per-session instance mutex and finishes an update on the relaunched process.</summary>
public partial class App
{
    private ApplicationInstanceMutex? _instanceMutex;

    /// <summary>
    /// Acquires the instance mutex the updater and the installer wait on. A process started with
    /// <c>--updated</c> waits for the previous instance to exit first, then removes the swap leftovers.
    /// The mutex is held for the process lifetime and released by process exit.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    private void AcquireInstanceMutex(string[] arguments)
    {

        var updated = arguments.Contains(PortableUpdateApplier.UPDATED_ARGUMENT, StringComparer.OrdinalIgnoreCase);
        var mutex = new ApplicationInstanceMutex();
        var owned = mutex.TryAcquire(updated ? UpdateStartup.PREVIOUS_INSTANCE_WAIT : TimeSpan.Zero);

        _instanceMutex = mutex;

        if (updated && !owned)
            Log.Warning("Previous instance did not exit within {Seconds} s; update cleanup is deferred", UpdateStartup.PREVIOUS_INSTANCE_WAIT.TotalSeconds);

        if (!owned)
            return;

        var files = new UpdateSwapJournal().CleanupAfterUpdate();

        if (files > 0 || updated)
            Log.Information("UpdateCleanupCompleted {Files}", files);
    }
}
