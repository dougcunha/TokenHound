using AwesomeAssertions;
using System.Collections.Generic;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Presentation;

/// <summary>
/// Verifies placement state transitions and persistence performed by <see cref="HudPlacementService"/>.
/// </summary>
public sealed class HudPlacementServiceTests
{
    private static readonly DisplayInfo PRIMARY = CreateDisplay(1, isPrimary: true, left: 0);
    private static readonly DisplayInfo SECONDARY = CreateDisplay(2, isPrimary: false, left: 1920);
    private static readonly HudDisplayPreference SECONDARY_PREFERENCE = DisplayResolver.ToPreference(SECONDARY);

    private readonly List<HudPositionSettings> _saved = [];

    [Fact]
    public void RecordDrag_SwitchesToFreeAndKeepsDisplay()
    {

        var service = CreateService(new HudPositionSettings { Mode = "RightEdge", Display = SECONDARY_PREFERENCE });

        service.RecordDrag(640, 220).Should().BeTrue();

        service.Mode.Should().Be(HudDockMode.Free);
        service.Current.Left.Should().Be(640);
        service.Current.Top.Should().Be(220);
        service.Current.Display.Should().Be(SECONDARY_PREFERENCE);
        _saved.Should().ContainSingle();
    }

    [Fact]
    public void SelectMode_FromFreeOnNonPreferredDisplay_PrefersHostingDisplay()
    {

        var service = CreateService(new HudPositionSettings { Mode = "Free", Left = 2400, Top = 300 });

        service.SelectMode(HudDockMode.TopRight, Context(SECONDARY)).Should().BeTrue();

        service.Mode.Should().Be(HudDockMode.TopRight);
        service.Current.Display.Should().Be(SECONDARY_PREFERENCE);
        _saved.Should().ContainSingle();
    }

    [Fact]
    public void SelectMode_FromFreeOnPrimaryUnderPrimaryPreference_KeepsFollowingPrimary()
    {

        var service = CreateService(new HudPositionSettings { Left = 400, Top = 300 });

        service.SelectMode(HudDockMode.TopRight, Context(PRIMARY));

        service.Mode.Should().Be(HudDockMode.TopRight);
        service.Current.Display.Should().BeNull();
    }

    [Fact]
    public void SelectMode_BetweenDockedModes_KeepsPreferenceRegardlessOfHosting()
    {

        var service = CreateService(new HudPositionSettings { Mode = "TopCenter" });

        service.SelectMode(HudDockMode.LeftEdge, Context(SECONDARY));

        service.Mode.Should().Be(HudDockMode.LeftEdge);
        service.Current.Display.Should().BeNull();
    }

    [Fact]
    public void SelectMode_Free_StoresCurrentWindowPositionAndKeepsDisplay()
    {

        var service = CreateService(new HudPositionSettings { Mode = "TopRight", Display = SECONDARY_PREFERENCE, Left = 1, Top = 1 });

        service.SelectMode(HudDockMode.Free, Context(SECONDARY, left: 3500, top: 0));

        service.Mode.Should().Be(HudDockMode.Free);
        service.Current.Left.Should().Be(3500);
        service.Current.Top.Should().Be(0);
        service.Current.Display.Should().Be(SECONDARY_PREFERENCE);
    }

    [Fact]
    public void SelectMode_WhenAlreadyStored_DoesNotSaveOrNotify()
    {

        var service = CreateService(new HudPositionSettings { Mode = "RightEdge" });
        var notifications = 0;
        service.Changed += (_, _) => notifications++;

        service.SelectMode(HudDockMode.RightEdge, Context(PRIMARY)).Should().BeTrue();
        service.SelectDisplay(null).Should().BeTrue();

        _saved.Should().BeEmpty();
        notifications.Should().Be(0);
    }

    [Fact]
    public void SelectMode_TopCenterOverUnknownStoredMode_PersistsTheChoice()
    {

        var service = CreateService(new HudPositionSettings { Mode = "Diagonal" });

        service.SelectMode(HudDockMode.TopCenter, Context(PRIMARY));

        service.Current.Mode.Should().Be(nameof(HudDockMode.TopCenter));
        _saved.Should().ContainSingle();
    }

    [Fact]
    public void SelectDisplay_KeepsModeAndCoordinates()
    {

        var service = CreateService(new HudPositionSettings { Mode = "TopLeft", Left = 10, Top = 20 });

        service.SelectDisplay(SECONDARY_PREFERENCE);

        service.Current.Should().Be(new HudPositionSettings { Mode = "TopLeft", Left = 10, Top = 20, Display = SECONDARY_PREFERENCE });
    }

    [Fact]
    public void UpdateFreePosition_KeepsLegacyModeUnset()
    {

        var service = CreateService(new HudPositionSettings { Left = 9000, Top = 0 });

        service.UpdateFreePosition(3200, 0);

        service.Current.Mode.Should().BeNull();
        service.Current.Left.Should().Be(3200);
        service.Mode.Should().Be(HudDockMode.Free);
    }

    [Fact]
    public void Apply_WhenSaveFails_StillUpdatesStateAndNotifies()
    {

        var service = new HudPlacementService(new HudPositionSettings(), static _ => false);
        var notifications = 0;
        service.Changed += (_, _) => notifications++;

        service.RecordDrag(10, 10).Should().BeFalse();

        service.Mode.Should().Be(HudDockMode.Free);
        notifications.Should().Be(1);
    }

    private static HudPlacementContext Context(DisplayInfo hosting, double left = 0, double top = 0)
        => new()
        {
            Displays = [PRIMARY, SECONDARY],
            Hosting = hosting,
            Left = left,
            Top = top
        };

    private static DisplayInfo CreateDisplay(int number, bool isPrimary, double left)
        => new()
        {
            DevicePath = $@"\\?\DISPLAY#MON{number}#port#{{e6f07b5f}}",
            EdidKey = $"0000:000{number}",
            Name = $"Display {number}",
            Number = number,
            IsPrimary = isPrimary,
            WorkArea = new ScreenBounds { Left = left, Top = 0, Width = 1920, Height = 1040 },
            Width = 1920,
            Height = 1080
        };

    private HudPlacementService CreateService(HudPositionSettings initial)
        => new(initial, settings =>
        {

            _saved.Add(settings);

            return true;
        });
}
