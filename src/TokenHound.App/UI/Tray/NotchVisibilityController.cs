namespace TokenHound.App.UI.Tray;

/// <summary>
/// Tracks Notch visibility as the single source of truth and drives exactly one show or hide action
/// per transition. Also holds the one-way flag that lets a coordinated shutdown bypass Notch close interception.
/// </summary>
public sealed class NotchVisibilityController
{
    private readonly Action _showNotch;
    private readonly Action _hideNotch;

    private bool _isNotchVisible;
    private bool _shutdownRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotchVisibilityController"/> class.
    /// </summary>
    /// <param name="showNotch">Invoked once when the Notch transitions from hidden to visible.</param>
    /// <param name="hideNotch">Invoked once when the Notch transitions from visible to hidden.</param>
    /// <param name="initiallyVisible">The visibility state to seed; defaults to visible.</param>
    public NotchVisibilityController(Action showNotch, Action hideNotch, bool initiallyVisible = true)
    {

        ArgumentNullException.ThrowIfNull(showNotch);
        ArgumentNullException.ThrowIfNull(hideNotch);

        _showNotch = showNotch;
        _hideNotch = hideNotch;
        _isNotchVisible = initiallyVisible;
    }

    /// <summary>
    /// Occurs after every visibility transition.
    /// </summary>
    public event EventHandler? VisibilityChanged;

    /// <summary>
    /// Gets a value indicating whether the Notch is currently visible.
    /// </summary>
    public bool IsNotchVisible
        => _isNotchVisible;

    /// <summary>
    /// Gets a value indicating whether a Notch close request should be intercepted and turned into a hide.
    /// Becomes <see langword="false"/> permanently after <see cref="AllowClose"/> is called.
    /// </summary>
    public bool ShouldInterceptClose
        => !_shutdownRequested;

    /// <summary>
    /// Makes the Notch visible. No-op when it is already visible; otherwise invokes the show action once
    /// and raises <see cref="VisibilityChanged"/> once.
    /// </summary>
    public void Show()
    {

        if (_isNotchVisible)
            return;

        _isNotchVisible = true;
        _showNotch();
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Hides the Notch. No-op when it is already hidden; otherwise invokes the hide action once
    /// and raises <see cref="VisibilityChanged"/> once.
    /// </summary>
    public void Hide()
    {

        if (!_isNotchVisible)
            return;

        _isNotchVisible = false;
        _hideNotch();
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Performs exactly one visibility transition: hides the Notch when visible, shows it when hidden.
    /// </summary>
    public void Toggle()
    {

        if (_isNotchVisible)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// Permanently disables Notch close interception so a coordinated shutdown can close the window.
    /// One-way: subsequent calls have no additional effect.
    /// </summary>
    public void AllowClose()
        => _shutdownRequested = true;
}
