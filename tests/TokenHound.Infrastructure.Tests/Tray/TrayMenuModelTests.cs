using AwesomeAssertions;
using System.Linq;
using TokenHound.App.UI.Tray;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Tray;

/// <summary>
/// Verifies the tray context-menu descriptor order, separator placement, header copy, and the
/// visibility-toggle header resolver (TC-02, TC-04 T01 half).
/// </summary>
public sealed class TrayMenuModelTests
{
    /// <summary>
    /// Verifies that BuildDescriptor(true) yields the five entries in fixed order with the "Hide Notch"
    /// toggle header, a separator only before Exit, and every header equal to its copy constant (TC-02).
    /// </summary>
    [Fact]
    public void BuildDescriptor_WhenNotchVisible_ReturnsOrderedEntriesWithHideHeaderAndExitSeparator()
    {

        var entries = new TrayMenuModel().BuildDescriptor(true);

        entries.Select(static e => e.Key).Should().Equal(new[]
        {
            TrayMenuItemKey.ToggleNotch,
            TrayMenuItemKey.RefreshNow,
            TrayMenuItemKey.Settings,
            TrayMenuItemKey.About,
            TrayMenuItemKey.Exit,
        });

        entries.Single(static e => e.Key == TrayMenuItemKey.ToggleNotch).Header.Should().Be("Hide Notch");
        entries.Single(static e => e.Key == TrayMenuItemKey.RefreshNow).Header.Should().Be(TrayMenuModel.REFRESH_HEADER);
        entries.Single(static e => e.Key == TrayMenuItemKey.Settings).Header.Should().Be(TrayMenuModel.SETTINGS_HEADER);
        entries.Single(static e => e.Key == TrayMenuItemKey.About).Header.Should().Be(TrayMenuModel.ABOUT_HEADER);
        entries.Single(static e => e.Key == TrayMenuItemKey.Exit).Header.Should().Be(TrayMenuModel.EXIT_HEADER);

        entries.Single(static e => e.Key == TrayMenuItemKey.Exit).PrecededBySeparator.Should().BeTrue();
        entries.Where(static e => e.Key != TrayMenuItemKey.Exit)
            .Should().OnlyContain(static e => !e.PrecededBySeparator);
    }

    /// <summary>
    /// Verifies that BuildDescriptor(false) matches the visible descriptor except for the "Show Notch"
    /// toggle header (TC-02).
    /// </summary>
    [Fact]
    public void BuildDescriptor_WhenNotchHidden_DiffersOnlyByToggleHeader()
    {

        var model = new TrayMenuModel();

        var visible = model.BuildDescriptor(true);
        var hidden = model.BuildDescriptor(false);

        hidden.Select(static e => e.Key).Should().Equal(visible.Select(static e => e.Key));
        hidden.Single(static e => e.Key == TrayMenuItemKey.ToggleNotch).Header.Should().Be("Show Notch");
        hidden.Where(static e => e.Key != TrayMenuItemKey.ToggleNotch)
            .Should().Equal(visible.Where(static e => e.Key != TrayMenuItemKey.ToggleNotch));
    }

    /// <summary>
    /// Verifies that ResolveToggleHeader returns "Hide Notch" when visible and "Show Notch" when hidden (TC-04).
    /// </summary>
    [Fact]
    public void ResolveToggleHeader_ReturnsHideWhenVisibleAndShowWhenHidden()
    {

        TrayMenuModel.ResolveToggleHeader(true).Should().Be("Hide Notch");
        TrayMenuModel.ResolveToggleHeader(false).Should().Be("Show Notch");
    }

    /// <summary>
    /// Verifies that the UPPER_CASE copy constants carry the approved menu text, including the ellipsis
    /// code point in the Settings and About headers (TC-02, NFR-10).
    /// </summary>
    [Fact]
    public void CopyConstants_CarryApprovedMenuText()
    {

        TrayMenuModel.TOOLTIP.Should().Be("TokenHound");
        TrayMenuModel.HIDE_NOTCH_HEADER.Should().Be("Hide Notch");
        TrayMenuModel.SHOW_NOTCH_HEADER.Should().Be("Show Notch");
        TrayMenuModel.REFRESH_HEADER.Should().Be("Refresh Now");
        TrayMenuModel.SETTINGS_HEADER.Should().Be("Settings" + (char)0x2026);
        TrayMenuModel.ABOUT_HEADER.Should().Be("About" + (char)0x2026);
        TrayMenuModel.EXIT_HEADER.Should().Be("Exit");
    }
}
