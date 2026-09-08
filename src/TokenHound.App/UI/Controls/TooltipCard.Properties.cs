using System.Collections.Generic;
using System.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;

namespace TokenHound.App.UI.Controls;

public sealed partial class TooltipCard
{
    /// <summary>Identifies the <see cref="ProviderName"/> dependency property.</summary>
    public static readonly DependencyProperty ProviderNameProperty =
        RegisterProperty(nameof(ProviderName), string.Empty);

    /// <summary>Identifies the <see cref="Status"/> dependency property.</summary>
    public static readonly DependencyProperty StatusProperty =
        RegisterProperty(nameof(Status), ProviderStatus.Ok);

    /// <summary>Identifies the <see cref="Rows"/> dependency property.</summary>
    public static readonly DependencyProperty RowsProperty =
        RegisterProperty<IReadOnlyList<ProviderUsageRow>?>(nameof(Rows), null);

    /// <summary>Identifies the <see cref="SessionUsedFraction"/> dependency property.</summary>
    public static readonly DependencyProperty SessionUsedFractionProperty =
        RegisterProperty<double?>(nameof(SessionUsedFraction), null);

    /// <summary>Identifies the <see cref="SessionResetText"/> dependency property.</summary>
    public static readonly DependencyProperty SessionResetTextProperty =
        RegisterProperty<string?>(nameof(SessionResetText), null);

    /// <summary>Identifies the <see cref="WeeklyUsedFraction"/> dependency property.</summary>
    public static readonly DependencyProperty WeeklyUsedFractionProperty =
        RegisterProperty<double?>(nameof(WeeklyUsedFraction), null);

    /// <summary>Identifies the <see cref="WeeklyResetText"/> dependency property.</summary>
    public static readonly DependencyProperty WeeklyResetTextProperty =
        RegisterProperty<string?>(nameof(WeeklyResetText), null);

    /// <summary>Identifies the <see cref="ActiveSessionText"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveSessionTextProperty =
        RegisterProperty<string?>(nameof(ActiveSessionText), null);

    /// <summary>Identifies the <see cref="StatusMessage"/> dependency property.</summary>
    public static readonly DependencyProperty StatusMessageProperty =
        RegisterProperty<string?>(nameof(StatusMessage), null);

    /// <summary>Gets or sets the display name of the provider.</summary>
    public string ProviderName
    {
        get
            => (string)GetValue(ProviderNameProperty);
        set
            => SetValue(ProviderNameProperty, value);
    }

    /// <summary>Gets or sets the health and operational status of the provider.</summary>
    public ProviderStatus Status
    {
        get
            => (ProviderStatus)GetValue(StatusProperty);
        set
            => SetValue(StatusProperty, value);
    }

    /// <summary>Gets or sets the collection of usage rows.</summary>
    public IReadOnlyList<ProviderUsageRow>? Rows
    {
        get
            => (IReadOnlyList<ProviderUsageRow>?)GetValue(RowsProperty);
        set
            => SetValue(RowsProperty, value);
    }

    /// <summary>Gets or sets the rolling session quota fraction, or null if unmeasured.</summary>
    public double? SessionUsedFraction
    {
        get
            => (double?)GetValue(SessionUsedFractionProperty);
        set
            => SetValue(SessionUsedFractionProperty, value);
    }

    /// <summary>Gets or sets the countdown or reset description for the session window.</summary>
    public string? SessionResetText
    {
        get
            => (string?)GetValue(SessionResetTextProperty);
        set
            => SetValue(SessionResetTextProperty, value);
    }

    /// <summary>Gets or sets the weekly quota fraction, or null if unmeasured.</summary>
    public double? WeeklyUsedFraction
    {
        get
            => (double?)GetValue(WeeklyUsedFractionProperty);
        set
            => SetValue(WeeklyUsedFractionProperty, value);
    }

    /// <summary>Gets or sets the countdown or reset description for the weekly window.</summary>
    public string? WeeklyResetText
    {
        get
            => (string?)GetValue(WeeklyResetTextProperty);
        set
            => SetValue(WeeklyResetTextProperty, value);
    }

    /// <summary>Gets or sets the active session process description, or null if idle.</summary>
    public string? ActiveSessionText
    {
        get
            => (string?)GetValue(ActiveSessionTextProperty);
        set
            => SetValue(ActiveSessionTextProperty, value);
    }

    /// <summary>Gets or sets the actionable status guidance message, or null if normal.</summary>
    public string? StatusMessage
    {
        get
            => (string?)GetValue(StatusMessageProperty);
        set
            => SetValue(StatusMessageProperty, value);
    }

    private static DependencyProperty RegisterProperty<T>(string name, T defaultValue)
        => DependencyProperty.Register(
            name,
            typeof(T),
            typeof(TooltipCard),
            new PropertyMetadata(defaultValue, OnVisualChanged)
        );

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => (d as TooltipCard)?.UpdateAll();

    private void OnControlLoaded(object sender, RoutedEventArgs e)
        => UpdateAll();
}
