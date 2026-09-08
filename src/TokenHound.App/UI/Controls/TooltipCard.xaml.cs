using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;

namespace TokenHound.App.UI.Controls;

/// <summary>
/// Displays a detailed hover telemetry card with quota and credit usage rows, countdowns, and guidance.
/// </summary>
public sealed partial class TooltipCard : UserControl
{
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

    /// <summary>Initializes a new instance of the <see cref="TooltipCard"/> class.</summary>
    public TooltipCard()
    {

        InitializeComponent();
        Loaded += OnControlLoaded;
        UpdateAll();
    }

    private void UpdateAll()
    {

        ProviderNameText.Text = string.IsNullOrWhiteSpace(ProviderName) ? "Provider" : ProviderName;

        UpdateStatusBadge();
        UpdateRows();
        UpdateActiveSession();
        UpdateStatusMessage();
    }

    private void UpdateRows()
    {

        if (Rows is { Count: > 0 })
        {
            RowsControl.ItemsSource = Rows;

            return;
        }

        RowsControl.ItemsSource = CreateLegacyFallbackRows();
    }

    private List<ProviderUsageRow> CreateLegacyFallbackRows()
    {

        var fallbackRows = new List<ProviderUsageRow>();

        if (SessionUsedFraction.HasValue || !string.IsNullOrWhiteSpace(SessionResetText))
            fallbackRows.Add(CreateLegacySessionRow());

        if (WeeklyUsedFraction.HasValue || !string.IsNullOrWhiteSpace(WeeklyResetText))
            fallbackRows.Add(CreateLegacyWeeklyRow());

        return fallbackRows;
    }

    private ProviderUsageRow CreateLegacySessionRow()
    {

        var sessionClamped = SessionUsedFraction.HasValue ? Math.Clamp(SessionUsedFraction.Value, 0.0, 1.0) : (double?)null;
        var primary = sessionClamped.HasValue
            ? $"{(int)Math.Round(sessionClamped.Value * 100.0)}% Used"
            : (!string.IsNullOrWhiteSpace(SessionResetText) && SessionResetText.Contains("requests", StringComparison.OrdinalIgnoreCase) ? SessionResetText : "Unmeasured");

        return new ProviderUsageRow
        {
            Key = "legacy:session",
            Label = "Current session (5h)",
            UsedFraction = SessionUsedFraction,
            PrimaryQuantityText = primary,
            ResetText = SessionResetText
        };
    }

    private ProviderUsageRow CreateLegacyWeeklyRow()
    {

        var weeklyClamped = WeeklyUsedFraction.HasValue ? Math.Clamp(WeeklyUsedFraction.Value, 0.0, 1.0) : (double?)null;

        return new ProviderUsageRow
        {
            Key = "legacy:weekly",
            Label = "Weekly limit (7d)",
            UsedFraction = WeeklyUsedFraction,
            PrimaryQuantityText = weeklyClamped.HasValue ? $"{(int)Math.Round(weeklyClamped.Value * 100.0)}% Used" : "Unmeasured",
            ResetText = WeeklyResetText
        };
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
