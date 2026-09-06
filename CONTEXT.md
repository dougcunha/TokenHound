# TokenHound

TokenHound is a lightweight Windows 11 desktop companion that monitors real-time quota, rate limits, and agent activity across local AI coding assistants.

## Language

**Provider**:
An external AI coding assistant service or tool whose quota and activity are monitored.
_Avoid_: Service, vendor, tool, integration

**Snapshot**:
An immutable point-in-time reading of a provider's status, quota windows, and active rate-limit blocks.
_Avoid_: State, status report, telemetry data, metric

**Limit Window**:
A distinct quota allocation bounded by a reset period (e.g., 5-hour rolling window or weekly budget).
_Avoid_: Quota, rate limit, bucket, tier

**Activity Monitor**:
A local mechanism that detects whether an AI assistant is actively executing, waiting for input, or idle.
_Avoid_: Process watcher, logger, tracker, daemon

**Notch**:
A floating, non-activating, click-through HUD capsule anchored to a screen edge displaying provider indicators.
_Avoid_: Window, overlay, toolbar, widget, popup

**Provider Ring**:
A circular visual element inside the Notch representing a provider's consumption fraction and liveness state.
_Avoid_: Gauge, progress bar, dial, icon

**Borrow-Don't-Own**:
The architectural principle of reading credentials and sessions directly from local developer tools without refreshing tokens or creating competing logins.
_Avoid_: Credential reuse, session hijacking, token scraping
