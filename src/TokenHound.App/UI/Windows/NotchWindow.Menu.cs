using System;
using System.Windows;
using System.Windows.Controls;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Windows;

public sealed partial class NotchWindow
{
    private void OnPositionClick(object sender, RoutedEventArgs e)
    {

        if (_placement is null || sender is not MenuItem { Tag: string tag })
            return;

        if (Enum.TryParse<HudDockMode>(tag, ignoreCase: false, out var mode))
            _placement.SelectMode(mode, CreatePlacementContext());
    }

    private void UpdatePositionChecks()
    {

        if (PositionMenuItem is null || _placement is null)
            return;

        var current = _placement.Mode.ToString();

        foreach (var item in PositionMenuItem.Items)
        {

            if (item is MenuItem { Tag: string tag } menuItem)
                menuItem.IsChecked = string.Equals(tag, current, StringComparison.Ordinal);
        }
    }
}
