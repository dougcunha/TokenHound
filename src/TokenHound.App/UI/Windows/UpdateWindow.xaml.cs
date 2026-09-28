using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TokenHound.App.Interop;
using TokenHound.App.ViewModels;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Modeless dialog showing an update check result and the Update now, Later, and Skip this version choices.
/// </summary>
public sealed partial class UpdateWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateWindow"/> class.
    /// </summary>
    public UpdateWindow()
    {

        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        DataContextChanged += OnDataContextChanged;
    }

    private UpdateViewModel? ViewModel
        => DataContext as UpdateViewModel;

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {

        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowPlacement.EnableDarkMode(hwnd);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {

        if (e.OldValue is UpdateViewModel oldViewModel)
            oldViewModel.CloseRequested -= OnCloseRequested;

        if (e.NewValue is UpdateViewModel newViewModel)
            newViewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
        => Close();

    private void OnUpdateNowButtonClick(object sender, RoutedEventArgs e)
        => ViewModel?.UpdateNow();

    private void OnLaterButtonClick(object sender, RoutedEventArgs e)
        => ViewModel?.Later();

    private void OnSkipButtonClick(object sender, RoutedEventArgs e)
    {

        if (ViewModel is { } viewModel)
            _ = viewModel.SkipAsync();
    }

    private void OnCancelDownloadButtonClick(object sender, RoutedEventArgs e)
        => ViewModel?.CancelDownload();

    private void OnCheckAgainButtonClick(object sender, RoutedEventArgs e)
        => ViewModel?.StartCheck();

    private void OnReleaseNotesClick(object sender, RoutedEventArgs e)
        => ViewModel?.OpenReleaseNotes();
}
