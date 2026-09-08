using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TokenHound.App.UI.Controls;

/// <summary>
/// Converts a fractional utilization value (0.0 to 1.0) to a status indicator fill brush.
/// </summary>
public sealed class UsedFractionToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush BAR_GREEN = CreateFrozenBrush(0x10, 0xB9, 0x81);
    private static readonly SolidColorBrush BAR_AMBER = CreateFrozenBrush(0xF5, 0x9E, 0x0B);
    private static readonly SolidColorBrush BAR_RED = CreateFrozenBrush(0xEF, 0x44, 0x44);

    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {

        if (value is double fraction)
        {

            if (fraction >= 0.9)
                return BAR_RED;

            if (fraction >= 0.7)
                return BAR_AMBER;

            return BAR_GREEN;
        }

        return BAR_GREEN;
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {

        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();

        return brush;
    }
}
