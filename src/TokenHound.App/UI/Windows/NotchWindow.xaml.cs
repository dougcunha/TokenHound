using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using TokenHound.App.Interop;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Floating, non-activating screen-edge HUD capsule displaying provider telemetry rings.
/// </summary>
public sealed partial class NotchWindow : Window
{
    private const double TOP_OFFSET = 8.0;
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;

    private HudActionsViewModel? _actionsViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotchWindow"/> class.
    /// </summary>
    public NotchWindow()
    {

        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        CapsuleBorder.ContextMenuOpening += OnCapsuleContextMenuOpening;
    }

    /// <summary>
    /// Gets or sets the HUD actions view model coordinating menu commands and feedback.
    /// </summary>
    public HudActionsViewModel? ActionsViewModel
    {
        get => _actionsViewModel;
        set
        {

            if (ReferenceEquals(_actionsViewModel, value))
                return;

            if (_actionsViewModel is not null)
                _actionsViewModel.PropertyChanged -= OnActionsViewModelPropertyChanged;

            _actionsViewModel = value;

            if (_actionsViewModel is not null)
                _actionsViewModel.PropertyChanged += OnActionsViewModelPropertyChanged;

            UpdateStatusPopup();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowStyles.EnableNonActivating(hwnd);

        if (hwnd != IntPtr.Zero)
        {

            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
        }
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {

        if (msg == WM_MOUSEACTIVATE)
        {

            handled = true;

            return new IntPtr(MA_NOACTIVATE);
        }

        return IntPtr.Zero;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {

        RepositionTopCenter();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {

        RepositionTopCenter();
    }

    private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {

        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnCapsuleContextMenuOpening(object sender, ContextMenuEventArgs e)
    {

        if (RefreshMenuItem is not null && _actionsViewModel is not null)
            RefreshMenuItem.IsEnabled = !_actionsViewModel.IsRefreshing;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {

        _ = _actionsViewModel?.CloseAsync();
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {

        _ = _actionsViewModel?.RefreshAsync();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {

        _actionsViewModel?.ShowSettings();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {

        _actionsViewModel?.ShowAbout();
    }

    private void OnStatusPopupMouseDown(object? sender, MouseButtonEventArgs e)
    {

        if (_actionsViewModel is not null && !_actionsViewModel.IsRefreshing)
            _actionsViewModel.DismissStatus();
    }

    private void OnActionsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {

        if (string.Equals(e.PropertyName, nameof(HudActionsViewModel.IsRefreshing), StringComparison.OrdinalIgnoreCase))
        {

            Dispatcher.Invoke(() =>
            {

                if (RefreshMenuItem is not null && _actionsViewModel is not null)
                    RefreshMenuItem.IsEnabled = !_actionsViewModel.IsRefreshing;
            });
        }
        else if (string.Equals(e.PropertyName, nameof(HudActionsViewModel.IsStatusVisible), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(e.PropertyName, nameof(HudActionsViewModel.RefreshStatusText), StringComparison.OrdinalIgnoreCase))
        {

            Dispatcher.Invoke(UpdateStatusPopup);
        }
    }

    private void UpdateStatusPopup()
    {

        if (StatusPopup is null || _actionsViewModel is null)
            return;

        StatusText.Text = _actionsViewModel.RefreshStatusText ?? string.Empty;
        StatusPopup.IsOpen = _actionsViewModel.IsStatusVisible;
    }

    private void RepositionTopCenter()
    {

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + ((workArea.Width - ActualWidth) / 2.0);
        Top = workArea.Top + TOP_OFFSET;
    }
}
