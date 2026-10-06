using AwesomeAssertions;
using System.Collections.Generic;
using System.Linq;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies the display list, selection, and live apply behavior of <see cref="HudPlacementSettingsViewModel"/>.
/// </summary>
public sealed class HudPlacementSettingsViewModelTests
{
    private static readonly DisplayInfo PRIMARY = CreateDisplay(
        2,
        "CX156A",
        isPrimary: true,
        left: 0
    );

    private static readonly DisplayInfo ULTRAWIDE = CreateDisplay(
        3,
        "LG ULTRAWIDE",
        isPrimary: false,
        left: 1920
    );

    private static readonly DisplayInfo UNNAMED = CreateDisplay(
        1,
        "Display 1",
        isPrimary: false,
        left: -1920
    );

    private readonly List<HudPositionSettings> _saved = [];

    [Fact]
    public void Displays_ListPrimaryOptionThenEachConnectedDisplay()
    {

        var viewModel = CreateViewModel(new HudPositionSettings());

        viewModel.Displays.Select(static option => option.Label).Should().Equal(
            HudPlacementSettingsViewModel.PRIMARY_LABEL,
            "2 — CX156A · 1920×1080 · Primary",
            "3 — LG ULTRAWIDE · 1920×1080",
            "1 — Display 1 · 1920×1080"
        );
        viewModel.SelectedDisplay.Should().Be(viewModel.Displays[0]);
    }

    [Fact]
    public void Displays_WhenPreferredDisplayIsDisconnected_AddsAndSelectsItOnce()
    {

        var missing = new HudDisplayPreference { DevicePath = @"\\?\DISPLAY#GONE", EdidKey = "FFFF:0001", Name = "Old Dell" };

        var viewModel = CreateViewModel(new HudPositionSettings { Mode = "TopRight", Display = missing });

        viewModel.Displays.Count(static option => option.IsDisconnected).Should().Be(1);
        viewModel.SelectedDisplay.Label.Should().Be($"Old Dell{HudPlacementSettingsViewModel.DISCONNECTED_SUFFIX}");
        viewModel.SelectedDisplay.Preference.Should().Be(missing);
    }

    [Fact]
    public void SelectedDisplay_InitiallyMatchesStoredConnectedDisplay()
    {

        var viewModel = CreateViewModel(new HudPositionSettings { Mode = "TopRight", Display = DisplayResolver.ToPreference(ULTRAWIDE) });

        viewModel.SelectedDisplay.Label.Should().StartWith("3 — LG ULTRAWIDE");
        viewModel.SelectedMode.Mode.Should().Be(HudDockMode.TopRight);
        _saved.Should().BeEmpty();
    }

    [Fact]
    public void FreeMode_DisablesDisplaySelectorWithHint()
    {

        var viewModel = CreateViewModel(new HudPositionSettings { Left = 500, Top = 300 });

        viewModel.SelectedMode.Mode.Should().Be(HudDockMode.Free);
        viewModel.IsDisplayEnabled.Should().BeFalse();
        viewModel.DisplayHint.Should().Be(HudPlacementSettingsViewModel.FREE_HINT);
    }

    [Fact]
    public void SelectingMode_AppliesOnceAndEnablesDisplaySelector()
    {

        var viewModel = CreateViewModel(new HudPositionSettings { Left = 500, Top = 300 });

        viewModel.SelectedMode = viewModel.Modes.Single(static option => option.Mode == HudDockMode.LeftEdge);

        _saved.Should().ContainSingle().Which.Mode.Should().Be(nameof(HudDockMode.LeftEdge));
        viewModel.IsDisplayEnabled.Should().BeTrue();
        viewModel.DisplayHint.Should().BeNull();
    }

    [Fact]
    public void SelectingMode_FromFreeOnOtherDisplay_SelectsHostingDisplay()
    {

        var viewModel = CreateViewModel(new HudPositionSettings { Left = 2500, Top = 300 }, hosting: ULTRAWIDE);

        viewModel.SelectedMode = viewModel.Modes.Single(static option => option.Mode == HudDockMode.TopRight);

        viewModel.SelectedDisplay.Label.Should().StartWith("3 — LG ULTRAWIDE");
        _saved.Should().ContainSingle();
    }

    [Fact]
    public void SelectingDisplay_AppliesOnce()
    {

        var viewModel = CreateViewModel(new HudPositionSettings { Mode = "TopCenter" });

        viewModel.SelectedDisplay = viewModel.Displays.Single(static option => option.Label.StartsWith('3'));

        _saved.Should().ContainSingle().Which.Display.Should().Be(DisplayResolver.ToPreference(ULTRAWIDE));
    }

    [Fact]
    public void SelectingMode_WhenSaveFails_ShowsError()
    {

        var service = new HudPlacementService(new HudPositionSettings(), static _ => false);
        var viewModel = new HudPlacementSettingsViewModel(service, [PRIMARY], () => new HudPlacementContext());

        viewModel.SelectedMode = viewModel.Modes.Single(static option => option.Mode == HudDockMode.RightEdge);

        viewModel.SaveError.Should().Be(HudPlacementSettingsViewModel.SAVE_ERROR);
    }

    private static DisplayInfo CreateDisplay(
        int number,
        string name,
        bool isPrimary,
        double left
    )
        => new()
        {
            DevicePath = $@"\\?\DISPLAY#MON{number}#port#{{e6f07b5f}}",
            EdidKey = $"0000:000{number}",
            Name = name,
            Number = number,
            IsPrimary = isPrimary,
            WorkArea = new ScreenBounds { Left = left, Top = 0, Width = 1920, Height = 1040 },
            Width = 1920,
            Height = 1080
        };

    private HudPlacementSettingsViewModel CreateViewModel(HudPositionSettings initial, DisplayInfo? hosting = null)
    {

        var service = new HudPlacementService(initial, settings =>
        {

            _saved.Add(settings);

            return true;
        });

        DisplayInfo[] displays = [PRIMARY, ULTRAWIDE, UNNAMED];

        return new HudPlacementSettingsViewModel(
            service,
            displays,
            () => new HudPlacementContext { Displays = displays, Hosting = hosting ?? PRIMARY }
        );
    }
}
