using TokenHound.App.Interop;

namespace TokenHound.Infrastructure.Tests.Interop;

/// <summary>Protects the distinction between interactive HUD and passive shadow styles.</summary>
public sealed class HudContourStyleTests
{

    /// <summary>Checks that shadow composition preserves unrelated bits and is idempotent.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(0x00400000)]
    [InlineData(WindowStyles.WS_EX_LAYERED)]
    public void ShadowStyles_PreserveBitsAndPassInput(int initial)
    {

        var result = WindowStyles.ApplyShadowStyles(initial);
        Assert.Equal(initial, result & initial);
        Assert.True(WindowStyles.HasNonActivatingStyles(result));
        Assert.Equal(WindowStyles.WS_EX_TRANSPARENT, result & WindowStyles.WS_EX_TRANSPARENT);
        Assert.Equal(WindowStyles.WS_EX_LAYERED, result & WindowStyles.WS_EX_LAYERED);
        Assert.Equal(result, WindowStyles.ApplyShadowStyles(result));
    }

    /// <summary>Checks that normal HUD styling never adds input transparency.</summary>
    [Fact]
    public void MainStyles_RemainInteractive()
    {

        var result = WindowStyles.ApplyExtendedStyles(WindowStyles.WS_EX_LAYERED);
        Assert.Equal(0, result & WindowStyles.WS_EX_TRANSPARENT);
        Assert.Equal(WindowStyles.WS_EX_LAYERED, result & WindowStyles.WS_EX_LAYERED);
    }
}
