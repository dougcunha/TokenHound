using Serilog;
using TokenHound.App.Interop;
using TokenHound.App.Presentation;
using TokenHound.App.UI.Windows;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App;

/// <summary>Wires the HUD window to its placement service and display catalog.</summary>
public partial class App
{
    private readonly DisplayCatalog _displayCatalog = new();
    private readonly HudPlacementService _hudPlacement = HudPlacementService.Create(new HudPositionStore());

    private HudPlacementSettingsViewModel? CreateHudPlacementSettingsViewModel()
        => _notchWindow is { } notchWindow
            ? HudPlacementSettingsViewModel.Create(
                _hudPlacement,
                _displayCatalog.GetDisplays,
                notchWindow.CreatePlacementContext
            )
            : null;

    private void ShowNotchWindow(NotchViewModel notchViewModel, HudActionsViewModel actionsViewModel)
    {

        // Enumerating first logs the display layout the HUD docks against.
        _displayCatalog.GetDisplays();

        _notchWindow = new NotchWindow
        {
            DataContext = notchViewModel,
            ActionsViewModel = actionsViewModel,
            Placement = _hudPlacement,
            Displays = _displayCatalog
        };

        MainWindow = _notchWindow;
        _notchWindow.Show();

        Log.Information("HUD window displayed successfully.");
    }
}
