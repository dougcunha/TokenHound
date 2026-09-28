using AwesomeAssertions;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies the HUD popup "Check for Updates" action of <see cref="HudActionsViewModel"/> (FR-04).
/// </summary>
public sealed class HudActionsUpdateTests
{
    /// <summary>Verifies that the action runs once per call, is a no-op when unwired, and stops after shutdown.</summary>
    [Fact]
    public async Task CheckForUpdates_InvokesActionWhenWired_AndStopsAfterShutdown()
    {

        var calls = 0;
        var wired = new HudActionsViewModel(
            static _ => Task.CompletedTask,
            static () => Task.CompletedTask,
            static () => { },
            static () => { }
        )
        {
            CheckForUpdatesAction = () => calls++
        };
        var unwired = new HudActionsViewModel(
            static _ => Task.CompletedTask,
            static () => Task.CompletedTask,
            static () => { },
            static () => { }
        );

        wired.CheckForUpdates();
        wired.CheckForUpdates();
        unwired.CheckForUpdates();
        await wired.ShutdownAsync();
        wired.CheckForUpdates();

        calls.Should().Be(2);
    }
}
