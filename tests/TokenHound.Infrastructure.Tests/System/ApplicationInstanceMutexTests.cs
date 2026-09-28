using AwesomeAssertions;
using System;
using System.Threading;
using TokenHound.Infrastructure.System;
using Xunit;

namespace TokenHound.Infrastructure.Tests.System;

/// <summary>
/// Verifies acquisition, contention, release, and abandonment of <see cref="ApplicationInstanceMutex"/> (TC-16).
/// Each owner runs on its own thread because a mutex is re-entrant for its owning thread.
/// </summary>
public sealed class ApplicationInstanceMutexTests
{
    private readonly string _name = $"TokenHound.Tests.{Guid.NewGuid():N}";

    /// <summary>
    /// Verifies that a second instance cannot acquire the mutex while the first holds it, and can after release.
    /// </summary>
    [Fact]
    public void TryAcquire_WhileHeldElsewhere_TimesOutThenSucceedsAfterRelease()
    {

        using var released = new ManualResetEventSlim();
        using var acquired = new ManualResetEventSlim();
        var owner = new Thread(() =>
        {
            using var first = new ApplicationInstanceMutex(_name);
            first.TryAcquire(TimeSpan.Zero);
            acquired.Set();
            released.Wait();
        });
        owner.Start();
        acquired.Wait(TestContext.Current.CancellationToken);

        var contended = RunOnThread(() => TryAcquireAndRelease(TimeSpan.FromMilliseconds(100)));
        released.Set();
        owner.Join();
        var afterRelease = RunOnThread(() => TryAcquireAndRelease(TimeSpan.FromSeconds(5)));

        contended.Should().BeFalse();
        afterRelease.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a mutex abandoned by an exited owner counts as acquired.
    /// </summary>
    [Fact]
    public void TryAcquire_WhenAbandoned_CountsAsAcquired()
    {

        var owner = new Thread(() =>
        {
            var abandoned = new Mutex(false, _name);
            abandoned.WaitOne();
        });
        owner.Start();
        owner.Join();

        RunOnThread(() => TryAcquireAndRelease(TimeSpan.FromSeconds(5))).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that disposing releases ownership.
    /// </summary>
    [Fact]
    public void Dispose_ReleasesOwnership()
    {

        var mutex = new ApplicationInstanceMutex(_name);
        mutex.TryAcquire(TimeSpan.Zero).Should().BeTrue();
        mutex.IsOwned.Should().BeTrue();

        mutex.Dispose();

        mutex.IsOwned.Should().BeFalse();
        RunOnThread(() => TryAcquireAndRelease(TimeSpan.Zero)).Should().BeTrue();
    }

    private bool TryAcquireAndRelease(TimeSpan timeout)
    {

        using var mutex = new ApplicationInstanceMutex(_name);

        return mutex.TryAcquire(timeout);
    }

    private static bool RunOnThread(Func<bool> action)
    {

        var result = false;
        var thread = new Thread(() => result = action());
        thread.Start();
        thread.Join();

        return result;
    }
}
