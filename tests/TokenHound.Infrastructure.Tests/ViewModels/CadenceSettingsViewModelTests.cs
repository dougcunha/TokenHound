using AwesomeAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies input validation, error messages, reset, discard, apply ordering, and persistence-failure
/// safety for <see cref="CadenceSettingsViewModel"/> (TC-05 to TC-08, T04).
/// </summary>
public sealed class CadenceSettingsViewModelTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;
    private readonly RateLimitPolicy _rateLimitPolicy = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CadenceSettingsViewModelTests"/> class.
    /// </summary>
    public CadenceSettingsViewModelTests()
    {

        _tempDirectory = Path.Combine(Path.GetTempPath(), $"cadence_vm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "appsettings.json");
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (File.Exists(_settingsFilePath))
            File.SetAttributes(_settingsFilePath, FileAttributes.Normal);

        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    /// <summary>
    /// Verifies that the view model initializes clean state from defaults when no settings exist.
    /// </summary>
    [Fact]
    public void Constructor_LoadsDefaults_AndInitializesCleanState()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.ActiveIntervalSeconds.Should().Be(180);
        viewModel.IdleIntervalSeconds.Should().Be(300);
        viewModel.MinimumRetryFloorSeconds.Should().Be(60);
        viewModel.HasErrors.Should().BeFalse();
        viewModel.IsDirty.Should().BeFalse();
        viewModel.CanApply.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that entering an active interval below 30 seconds displays the error and disables Apply (TC-05).
    /// </summary>
    [Fact]
    public void ActiveInterval_WhenLessThanThirty_ReportsErrorAndDisablesApply()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.ActiveIntervalSeconds = 15;

        viewModel.ActiveIntervalError.Should().Be("Active interval must be at least 30 seconds.");
        viewModel.HasErrors.Should().BeTrue();
        viewModel.CanApply.Should().BeFalse();
        viewModel.ApplyCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that entering an idle interval shorter than active displays the relational error (TC-06).
    /// </summary>
    [Fact]
    public void IdleInterval_WhenShorterThanActive_ReportsRelationalError()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.ActiveIntervalSeconds = 300;
        viewModel.IdleIntervalSeconds = 180;

        viewModel.IdleIntervalError.Should().Be("Idle interval cannot be shorter than active interval.");
        viewModel.HasErrors.Should().BeTrue();
        viewModel.CanApply.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that entering a retry floor below 60 seconds or non-numeric displays the safety floor error.
    /// </summary>
    [Fact]
    public void RetryFloor_WhenBelowSixtyOrInvalid_ReportsFloorError()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.MinimumRetryFloorSeconds = 45;

        viewModel.RetryFloorError.Should().Be("Minimum retry floor must be at least 60 seconds to prevent API abuse.");
        viewModel.HasErrors.Should().BeTrue();

        viewModel.MinimumRetryFloorText = "not-a-number";
        viewModel.RetryFloorError.Should().Be("Minimum retry floor must be at least 60 seconds to prevent API abuse.");
    }

    /// <summary>
    /// Verifies that empty and non-numeric active and idle inputs produce inline validation errors.
    /// </summary>
    [Fact]
    public void ActiveAndIdle_WhenEmptyOrNonNumeric_ReportErrors()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.ActiveIntervalText = string.Empty;
        viewModel.ActiveIntervalError.Should().Be("Active interval must be at least 30 seconds.");

        viewModel.IdleIntervalText = "abc";
        viewModel.IdleIntervalError.Should().Be("Idle interval cannot be shorter than active interval.");
    }

    /// <summary>
    /// Verifies that changing active interval dynamically triggers revalidation of idle interval.
    /// </summary>
    [Fact]
    public void ActiveInterval_WhenChanged_DynamicallyRevalidatesIdleInterval()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.IdleIntervalSeconds = 200;
        viewModel.ActiveIntervalSeconds = 250;
        viewModel.IdleIntervalError.Should().Be("Idle interval cannot be shorter than active interval.");

        viewModel.ActiveIntervalSeconds = 150;
        viewModel.IdleIntervalError.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ResetToDefaults restores policy constants and clears validation errors (TC-07).
    /// </summary>
    [Fact]
    public void ResetToDefaults_RestoresPolicyConstants_AndClearsErrors()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.ActiveIntervalSeconds = 10;
        viewModel.IdleIntervalSeconds = 10;
        viewModel.MinimumRetryFloorSeconds = 10;
        viewModel.HasErrors.Should().BeTrue();

        viewModel.ResetToDefaults();

        viewModel.ActiveIntervalSeconds.Should().Be(180);
        viewModel.IdleIntervalSeconds.Should().Be(300);
        viewModel.MinimumRetryFloorSeconds.Should().Be(60);
        viewModel.HasErrors.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Discard reverts inputs to persisted values without mutating runtime state (TC-08).
    /// </summary>
    [Fact]
    public void Discard_RestoresPersistedSnapshot_WithoutMutatingRuntime()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        viewModel.ActiveIntervalSeconds = 60;
        viewModel.IdleIntervalSeconds = 90;
        viewModel.MinimumRetryFloorSeconds = 120;
        viewModel.IsDirty.Should().BeTrue();

        viewModel.Discard();

        viewModel.ActiveIntervalSeconds.Should().Be(180);
        viewModel.IdleIntervalSeconds.Should().Be(300);
        viewModel.MinimumRetryFloorSeconds.Should().Be(60);
        viewModel.IsDirty.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that valid edits persist to disk and immediately update runtime targets upon Apply.
    /// </summary>
    [Fact]
    public void Apply_WhenValid_PersistsSettingsAndUpdatesRuntime()
    {

        using var store = new UsageStore();
        var (viewModel, refreshStore, rateLimitStore) = CreateViewModel(store);

        viewModel.ActiveIntervalSeconds = 60;
        viewModel.IdleIntervalSeconds = 120;
        viewModel.MinimumRetryFloorSeconds = 90;

        viewModel.CanApply.Should().BeTrue();
        viewModel.Apply();

        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(60));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(120));
        _rateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(90));

        refreshStore.Load().ActiveIntervalSeconds.Should().Be(60);
        refreshStore.Load().IdleIntervalSeconds.Should().Be(120);
        rateLimitStore.Load().MinimumRetryFloorSeconds.Should().Be(90);

        viewModel.IsDirty.Should().BeFalse();
        viewModel.CanApply.Should().BeFalse();
        viewModel.HasApplyError.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that persistence failure leaves runtime state untouched, keeps dirty state, and reports error.
    /// </summary>
    [Fact]
    public void Apply_WhenPersistenceFails_DoesNotMutateRuntimeAndRetainsDirtyState()
    {

        using var store = new UsageStore();
        var (viewModel, _, _) = CreateViewModel(store);

        File.WriteAllText(_settingsFilePath, "{ \"Refresh\": { \"ActiveIntervalSeconds\": 180 } }");
        File.SetAttributes(_settingsFilePath, FileAttributes.ReadOnly);

        viewModel.ActiveIntervalSeconds = 60;
        viewModel.IdleIntervalSeconds = 120;
        viewModel.MinimumRetryFloorSeconds = 90;

        viewModel.Apply();

        viewModel.HasApplyError.Should().BeTrue();
        viewModel.ApplyError.Should().NotBeNullOrEmpty();
        viewModel.IsDirty.Should().BeTrue();

        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(180));
        _rateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that <see cref="SettingsViewModel"/> composes <see cref="CadenceSettingsViewModel"/>.
    /// </summary>
    [Fact]
    public void SettingsViewModel_ComposesCadenceSettingsViewModel()
    {

        using var store = new UsageStore();
        using var settingsVm = new SettingsViewModel(store, static (_, _) => Task.FromResult(true));

        settingsVm.Cadence.Should().NotBeNull();
        settingsVm.Cadence.ActiveIntervalSeconds.Should().Be(180);
    }

    private (CadenceSettingsViewModel Vm, RefreshSettingsStore Refresh, RateLimitSettingsStore RateLimit) CreateViewModel(
        UsageStore store)
    {

        var refreshStore = new RefreshSettingsStore(_settingsFilePath);
        var rateLimitStore = new RateLimitSettingsStore(_settingsFilePath);
        var vm = new CadenceSettingsViewModel(
            store,
            refreshStore,
            rateLimitStore,
            _rateLimitPolicy
        );

        return (vm, refreshStore, rateLimitStore);
    }
}
