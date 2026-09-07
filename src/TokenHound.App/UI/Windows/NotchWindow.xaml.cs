using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Serilog;
using TokenHound.App.Interop;
using TokenHound.App.UI.Placement;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Floating, non-activating screen-edge HUD capsule displaying provider telemetry rings.
/// </summary>
public sealed partial class NotchWindow : Window
{
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;

    private readonly HudPositionStore _positionStore = new();

    private HudActionsViewModel? _actionsViewModel;
    private HudPositionSettings _position = new();

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

        _position = _positionStore.Load();

        if (_position.TryGetPosition(out var left, out var top))
            Log.Debug("Restoring persisted HUD position Left={Left} Top={Top}", left, top);

        ApplyPlacement();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {

        ApplyPlacement();
    }

    private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {

        if (e.ButtonState != MouseButtonState.Pressed)
            return;

        DragMove();
        PersistPosition();
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

    private void ApplyPlacement()
    {

        if (!_position.TryGetPosition(out var left, out var top))
            (left, top) = NotchPlacement.CenterOnTopEdge(CurrentWorkArea(), ActualWidth);

        (Left, Top) = NotchPlacement.Clamp(
            CurrentVirtualScreen(),
            left,
            top,
            ActualWidth,
            ActualHeight
        );
    }

    private void PersistPosition()
    {

        var moved = new HudPositionSettings { Left = Left, Top = Top };

        if (moved == _position)
            return;

        _position = moved;

        if (_positionStore.Save(moved))
            Log.Debug("Persisted HUD position Left={Left} Top={Top}", moved.Left, moved.Top);
        else
            Log.Warning("Unable to persist HUD position to {SettingsFile}", _positionStore.FilePath);
    }

    private static ScreenBounds CurrentWorkArea()
    {

        var workArea = SystemParameters.WorkArea;

        return new ScreenBounds
        {
            Left = workArea.Left,
            Top = workArea.Top,
            Width = workArea.Width,
            Height = workArea.Height
        };
    }

    private static ScreenBounds CurrentVirtualScreen()
    {

        return new ScreenBounds
        {
            Left = SystemParameters.VirtualScreenLeft,
            Top = SystemParameters.VirtualScreenTop,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight
        };
    }
}
