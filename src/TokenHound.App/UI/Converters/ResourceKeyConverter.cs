using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TokenHound.App.UI.Converters;

/// <summary>
/// Resolves an application resource key carried by a view model into the resource it names.
/// View models expose brush and geometry keys as plain strings so they stay free of presentation
/// framework types; this converter performs the lookup on the view side.
/// </summary>
[ValueConversion(typeof(string), typeof(object))]
public sealed class ResourceKeyConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        if (value is not string resourceKey || string.IsNullOrWhiteSpace(resourceKey))
            return DependencyProperty.UnsetValue;

        return Application.Current?.TryFindResource(resourceKey) ?? DependencyProperty.UnsetValue;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Resource keys are resolved in one direction only.");
}
