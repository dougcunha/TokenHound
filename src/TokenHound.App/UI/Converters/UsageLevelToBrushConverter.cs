using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Converters;

/// <summary>
/// Converts a <see cref="UsageLevel"/> into the progress-bar brush of the matching ring colour state.
/// </summary>
[ValueConversion(typeof(UsageLevel), typeof(Brush))]
public sealed class UsageLevelToBrushConverter : IValueConverter
{
    /// <summary>Gets or sets the brush for <see cref="UsageLevel.Green"/>.</summary>
    public Brush GreenBrush { get; set; } = Brushes.Transparent;

    /// <summary>Gets or sets the brush for <see cref="UsageLevel.Yellow"/>.</summary>
    public Brush YellowBrush { get; set; } = Brushes.Transparent;

    /// <summary>Gets or sets the brush for <see cref="UsageLevel.Orange"/>.</summary>
    public Brush OrangeBrush { get; set; } = Brushes.Transparent;

    /// <summary>Gets or sets the brush for <see cref="UsageLevel.None"/> and unknown values.</summary>
    public Brush NoneBrush { get; set; } = Brushes.Transparent;

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            UsageLevel.Green => GreenBrush,
            UsageLevel.Yellow => YellowBrush,
            UsageLevel.Orange => OrangeBrush,
            _ => NoneBrush
        };

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Usage levels are converted in one direction only.");
}
