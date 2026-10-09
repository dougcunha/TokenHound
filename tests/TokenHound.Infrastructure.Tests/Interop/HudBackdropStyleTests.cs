using TokenHound.App.Interop;

namespace TokenHound.Infrastructure.Tests.Interop;

/// <summary>Protects the passive, non-activating styles of the backdrop companion (TC-03).</summary>
public sealed class HudBackdropStyleTests
{

    /// <summary>Checks that the companion passes input, never activates, and stays off the taskbar.</summary>
    [Fact]
    public void CompanionStyles_PassInputAndNeverActivate()
    {

        var style = HudBackdropInterop.EXTENDED_STYLE;
        Assert.Equal(WindowStyles.WS_EX_TRANSPARENT, style & WindowStyles.WS_EX_TRANSPARENT);
        Assert.Equal(WindowStyles.WS_EX_NOACTIVATE, style & WindowStyles.WS_EX_NOACTIVATE);
        Assert.Equal(WindowStyles.WS_EX_TOOLWINDOW, style & WindowStyles.WS_EX_TOOLWINDOW);
        Assert.Equal(WindowStyles.WS_EX_TOPMOST, style & WindowStyles.WS_EX_TOPMOST);
    }

    /// <summary>Checks that the companion draws only composition content and stays non-layered, as T01 proved.</summary>
    [Fact]
    public void CompanionStyles_UseCompositionWithoutLayering()
    {

        var style = HudBackdropInterop.EXTENDED_STYLE;
        Assert.Equal(HudBackdropInterop.WS_EX_NOREDIRECTIONBITMAP, style & HudBackdropInterop.WS_EX_NOREDIRECTIONBITMAP);
        Assert.Equal(0, style & WindowStyles.WS_EX_LAYERED);
    }
}
