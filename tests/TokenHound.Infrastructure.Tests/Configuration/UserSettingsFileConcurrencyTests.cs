using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Configuration;

/// <summary>
/// Verifies cancellation and migration coordination around the shared settings-file gate.
/// </summary>
public sealed class UserSettingsFileConcurrencyTests : IDisposable
{
    private readonly string _directory;
    private readonly string _defaultsPath;
    private readonly string _userPath;

    /// <summary>
    /// Initializes isolated settings paths for each test.
    /// </summary>
    public UserSettingsFileConcurrencyTests()
    {

        _directory = Path.Combine(Path.GetTempPath(), $"tokenhound-settings-race-{Guid.NewGuid():N}");
        _defaultsPath = Path.Combine(_directory, "defaults.json");
        _userPath = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Fact]
    public async Task LoadAsync_WhenConcurrentUpdateCreatesUserFile_PreservesUpdate()
    {

        File.WriteAllText(_defaultsPath, """{ "Hud": { "Left": 550.0, "Top": 220.0 } }""");
        var file = new UserSettingsFile(_userPath, _defaultsPath);
        using var blocker = new GateBlocker(file, static settings => settings with
        {
            Hud = new HudPositionSettings { Left = 999.0, Top = 333.0 }
        });
        await blocker.WaitUntilEnteredAsync(TestContext.Current.CancellationToken);

        var loadTask = file.LoadAsync(TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        blocker.Release();

        (await blocker.UpdateTask).Should().BeTrue();
        var loaded = await loadTask;

        loaded.Hud!.Left.Should().Be(999.0);
        file.Load().Hud!.Left.Should().Be(999.0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsyncOperation_WhenCancelledWhileWaitingForGate_Propagates(
        bool loadOperation)
    {

        var file = new UserSettingsFile(_userPath, _defaultsPath);
        using var blocker = new GateBlocker(file, static settings => settings);
        await blocker.WaitUntilEnteredAsync(TestContext.Current.CancellationToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        Task operation = loadOperation
            ? file.LoadAsync(cancellation.Token)
            : file.SaveAsync(new UserSettings(), cancellation.Token);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await operation);
        blocker.Release();
        (await blocker.UpdateTask).Should().BeTrue();
    }

    private sealed class GateBlocker : IDisposable
    {
        private readonly TaskCompletionSource _entered = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        private readonly ManualResetEventSlim _release = new();

        internal GateBlocker(
            UserSettingsFile file,
            Func<UserSettings, UserSettings> updateAction)
        {

            UpdateTask = Task.Run(() => file.Update(settings =>
            {
                _entered.TrySetResult();
                _release.Wait(TestContext.Current.CancellationToken);

                return updateAction(settings);
            }), TestContext.Current.CancellationToken);
        }

        internal Task<bool> UpdateTask { get; }

        internal Task WaitUntilEnteredAsync(CancellationToken cancellationToken)
            => _entered.Task.WaitAsync(cancellationToken);

        internal void Release()
            => _release.Set();

        /// <inheritdoc />
        public void Dispose()
        {

            _release.Set();
            _release.Dispose();
        }
    }
}
