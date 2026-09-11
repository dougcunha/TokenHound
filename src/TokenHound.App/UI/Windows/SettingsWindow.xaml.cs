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
/// Modeless dialog displaying the application settings surface for provider management and cadence.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    public SettingsWindow()
    {

        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {

        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {

            e.Handled = true;
            OnCancel();

            return;
        }

        if (e.Key == Key.Enter)
        {

            if (SettingsTabControl.SelectedIndex != 1)
                return;

            if (FocusManager.GetFocusedElement(this) is Button button && button != ApplyButton)
                return;

            if (DataContext is SettingsViewModel vm && vm.Cadence.CanApply)
            {

                e.Handled = true;
                vm.Cadence.Apply();

                if (!vm.Cadence.HasApplyError)
                    ShowAppliedFeedback();
            }
        }
    }

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e)
    {

        base.OnClosing(e);
        DiscardChanges();
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {

        base.OnClosed(e);

        if (DataContext is SettingsViewModel vm)
            vm.Cadence.PropertyChanged -= OnCadencePropertyChanged;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowPlacement.EnableDarkMode(hwnd);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {

        if (ActiveIntervalTextBox.IsVisible)
        {

            ActiveIntervalTextBox.Focus();
            ActiveIntervalTextBox.SelectAll();
        }
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (!ReferenceEquals(e.Source, SettingsTabControl))
            return;

        StatusMessageTextBlock.Visibility = Visibility.Collapsed;

        if (SettingsTabControl.SelectedIndex == 1 && ActiveIntervalTextBox.IsVisible)
        {

            ActiveIntervalTextBox.Focus();
            ActiveIntervalTextBox.SelectAll();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {

        if (e.OldValue is SettingsViewModel oldVm)
            oldVm.Cadence.PropertyChanged -= OnCadencePropertyChanged;

        if (e.NewValue is SettingsViewModel newVm)
            newVm.Cadence.PropertyChanged += OnCadencePropertyChanged;
    }

    private void OnCadencePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {

        if (e.PropertyName is not null)
            StatusMessageTextBlock.Visibility = Visibility.Collapsed;
    }

    private void OnResetButtonClick(object sender, RoutedEventArgs e)
    {

        if (DataContext is SettingsViewModel vm)
        {

            vm.Cadence.ResetToDefaults();
            StatusMessageTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {

        OnCancel();
    }

    private void OnApplyButtonClick(object sender, RoutedEventArgs e)
    {

        if (DataContext is SettingsViewModel vm && !vm.Cadence.HasApplyError)
            ShowAppliedFeedback();
    }

    private void OnCancel()
    {

        DiscardChanges();
        Close();
    }

    private void DiscardChanges()
    {

        if (DataContext is SettingsViewModel vm)
            vm.Cadence.Discard();
    }

    private void ShowAppliedFeedback()
    {

        StatusMessageTextBlock.Visibility = Visibility.Visible;
    }
}

