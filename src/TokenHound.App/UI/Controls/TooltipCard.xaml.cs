using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TokenHound.Core.Models;

namespace TokenHound.App.UI.Controls;

/// <summary>
/// Displays a detailed hover telemetry card with rolling session and weekly quotas, countdowns, and guidance.
/// </summary>
public sealed partial class TooltipCard : UserControl
{
    private static readonly SolidColorBrush BAR_GREEN = CreateFrozenBrush(0x10, 0xB9, 0x81);
    private static readonly SolidColorBrush BAR_AMBER = CreateFrozenBrush(0xF5, 0x9E, 0x0B);
    private static readonly SolidColorBrush BAR_RED = CreateFrozenBrush(0xEF, 0x44, 0x44);

    private static readonly SolidColorBrush TEXT_MUTED = CreateFrozenBrush(0x71, 0x71, 0x7A);
    private static readonly SolidColorBrush TEXT_PRIMARY = CreateFrozenBrush(0xE4, 0xE4, 0xE7);

    private static readonly (SolidColorBrush Bg, SolidColorBrush Fg) BADGE_OK =
        (CreateFrozenBrush(0x06, 0x4E, 0x3B), CreateFrozenBrush(0x34, 0xD3, 0x99));
    private static readonly (SolidColorBrush Bg, SolidColorBrush Fg) BADGE_STALE =
        (CreateFrozenBrush(0x27, 0x27, 0x2A), CreateFrozenBrush(0x9C, 0xA3, 0xAF));
    private static readonly (SolidColorBrush Bg, SolidColorBrush Fg) BADGE_AUTH =
        (CreateFrozenBrush(0x3B, 0x1B, 0x54), CreateFrozenBrush(0xC0, 0x84, 0xFC));
    private static readonly (SolidColorBrush Bg, SolidColorBrush Fg) BADGE_ERR =
        (CreateFrozenBrush(0x45, 0x0A, 0x0A), CreateFrozenBrush(0xF8, 0x71, 0x71));

    private static readonly (SolidColorBrush Bg, SolidColorBrush Border, SolidColorBrush Fg) MSG_AUTH =
        (CreateFrozenBrush(0x24, 0x12, 0x35), CreateFrozenBrush(0x4C, 0x1D, 0x95), CreateFrozenBrush(0xDD, 0xD6, 0xFE));
    private static readonly (SolidColorBrush Bg, SolidColorBrush Border, SolidColorBrush Fg) MSG_ERR =
        (CreateFrozenBrush(0x2D, 0x15, 0x15), CreateFrozenBrush(0x7F, 0x1D, 0x1D), CreateFrozenBrush(0xFC, 0xA5, 0xA5));
    private static readonly (SolidColorBrush Bg, SolidColorBrush Border, SolidColorBrush Fg) MSG_DEF =
        (CreateFrozenBrush(0x18, 0x18, 0x1B), CreateFrozenBrush(0x27, 0x27, 0x2A), TEXT_MUTED);

    /// <summary>Identifies the <see cref="ProviderName"/> dependency property.</summary>
    public static readonly DependencyProperty ProviderNameProperty =
        RegisterProperty(nameof(ProviderName), string.Empty);

    /// <summary>Identifies the <see cref="Status"/> dependency property.</summary>
    public static readonly DependencyProperty StatusProperty =
        RegisterProperty(nameof(Status), ProviderStatus.Ok);

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

    /// <summary>Initializes a new instance of the <see cref="TooltipCard"/> class.</summary>
    public TooltipCard()
    {

        InitializeComponent();
        Loaded += OnControlLoaded;
        SessionBarTrack.SizeChanged += OnTrackSizeChanged;
        WeeklyBarTrack.SizeChanged += OnTrackSizeChanged;
        UpdateAll();
    }

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

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
    {

        UpdateQuotaBar(SessionUsedFraction, SessionBarTrack, SessionBarFill, SessionPercentText);
        UpdateQuotaBar(WeeklyUsedFraction, WeeklyBarTrack, WeeklyBarFill, WeeklyPercentText);
    }

    private void UpdateAll()
    {

        ProviderNameText.Text = string.IsNullOrWhiteSpace(ProviderName) ? "Provider" : ProviderName;
        SessionResetTextBlock.Text = SessionResetText ?? string.Empty;
        WeeklyResetTextBlock.Text = WeeklyResetText ?? string.Empty;

        UpdateStatusBadge();
        UpdateQuotaBar(SessionUsedFraction, SessionBarTrack, SessionBarFill, SessionPercentText);
        UpdateQuotaBar(WeeklyUsedFraction, WeeklyBarTrack, WeeklyBarFill, WeeklyPercentText);
        UpdateActiveSession();
        UpdateStatusMessage();
    }

    private void UpdateStatusBadge()
    {

        StatusBadgeText.Text = FormatStatus(Status);
        var (bg, fg) = Status switch
        {
            ProviderStatus.Ok => BADGE_OK,
            ProviderStatus.Stale => BADGE_STALE,
            ProviderStatus.NeedsAuth => BADGE_AUTH,
            _ => BADGE_ERR
        };
        StatusBadge.Background = bg;
        StatusBadgeText.Foreground = fg;
    }

    private static void UpdateQuotaBar(
        double? fraction,
        Grid track,
        Border fill,
        TextBlock label)
    {

        if (!fraction.HasValue)
        {
            fill.Visibility = Visibility.Collapsed;
            label.Text = "Unmeasured";
            label.Foreground = TEXT_MUTED;
            return;
        }

        var clamped = Math.Clamp(fraction.Value, 0.0, 1.0);
        var trackWidth = track.ActualWidth;

        if (trackWidth > 0)
            fill.Width = trackWidth * clamped;

        fill.Background = ResolveBarBrush(clamped);
        fill.Visibility = clamped > 0.0 ? Visibility.Visible : Visibility.Collapsed;
        label.Text = $"{(int)Math.Round(clamped * 100.0)}% Used";
        label.Foreground = TEXT_PRIMARY;
    }

    private void UpdateActiveSession()
    {

        if (string.IsNullOrWhiteSpace(ActiveSessionText))
        {
            ActiveSessionPanel.Visibility = Visibility.Collapsed;
            return;
        }

        ActiveSessionPanel.Visibility = Visibility.Visible;
        ActiveSessionTextBlock.Text = ActiveSessionText;
    }

    private void UpdateStatusMessage()
    {

        if (string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessagePanel.Visibility = Visibility.Collapsed;
            return;
        }

        StatusMessagePanel.Visibility = Visibility.Visible;
        StatusMessageTextBlock.Text = StatusMessage;
        var (bg, border, fg) = Status switch
        {
            ProviderStatus.NeedsAuth => MSG_AUTH,
            ProviderStatus.RateLimited or ProviderStatus.AccessDenied => MSG_ERR,
            _ => MSG_DEF
        };
        StatusMessagePanel.Background = bg;
        StatusMessagePanel.BorderBrush = border;
        StatusMessageTextBlock.Foreground = fg;
    }

    private static SolidColorBrush ResolveBarBrush(double fraction)
    {

        if (fraction >= 0.9)
            return BAR_RED;

        if (fraction >= 0.7)
            return BAR_AMBER;

        return BAR_GREEN;
    }

    private static string FormatStatus(ProviderStatus status)
        => status switch
        {
            ProviderStatus.Ok => "OK",
            ProviderStatus.Stale => "STALE",
            ProviderStatus.NeedsAuth => "NEEDS AUTH",
            ProviderStatus.AccessDenied => "ACCESS DENIED",
            ProviderStatus.RateLimited => "RATE LIMITED",
            _ => status.ToString().ToUpperInvariant()
        };

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {

        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();

        return brush;
    }
}
