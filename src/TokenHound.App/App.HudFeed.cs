using Serilog;
using TokenHound.App.HudFeed;
using TokenHound.App.Presentation;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.HudFeed;

namespace TokenHound.App;

/// <summary>Starts the HUD feed file that other local apps, such as a Herdr mod, read.</summary>
public partial class App
{
    private void StartHudFeed(NotchViewModel notchViewModel)
    {

        if (_lifetime is null)
            return;

        var writer = new HudFeedWriter();
        var publisher = new HudFeedPublisher(
            notchViewModel.Rings,
            writer.WriteAsync,
            DispatchUiAction,
            ApplicationInfo.Current.DisplayVersion
        );

        _lifetime.TrackAsyncResource(publisher);
        publisher.Start();
        Log.Information("HUD feed publishing to {HudFeedPath}", writer.FilePath);
    }
}
