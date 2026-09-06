# Technical Specification: Authentication Flows and Session Management

This document details the technical mechanics of how **authentication** operates across each assistant, how credentials are borrowed by TokenHound, and how underlying login flows function under the hood on **Windows 11**.

---

## 1. Authentication Philosophy: *Borrow-Don't-Own*

TokenHound deliberately adopts the **"Borrow, Don't Own"** architecture.

### Why the Application Avoids Standalone Independent Logins
During the original conception of the project, a preliminary version attempted an isolated web login for Cursor directly inside a WebView. The unintended consequence was the creation of a **second empty account**, causing the HUD to report 0% usage for an account different from the one the developer was actively using in their editor.

For this reason, the golden invariants of the system are:
1. **Never create competing parallel accounts**.
2. **Borrow existing credentials that official developer tools already created on Windows**.
3. **Never write or overwrite tokens** owned by other tools.

---

## 2. How TokenHound Authenticates to Each Provider

To gather metrics without requesting manual developer sign-ins, TokenHound extracts and injects existing credentials according to the table below:

| Provider | Extracted Credential Type | Injection Mechanism | Headers / Format |
| :--- | :--- | :--- | :--- |
| **Claude Code** | OAuth 2.0 Access Token | HTTP Authorization Header | `Authorization: Bearer <accessToken>`<br>`anthropic-beta: oauth-2025-04-20` |
| **Cursor** | Session Token + Account ID | Mandatory HTTP Cookie | `Cookie: WorkosCursorSessionToken=<AccountID>::<AccessToken>` *(Rejects Bearer!)* |
| **OpenAI Codex** | Local IPC Channel / Zero Network | Stdio JSON-RPC 2.0 | Local handshake (`initialize` and `account/rateLimits/read`) with `codex app-server` |
| **Antigravity (Local)** | Ephemeral CSRF Token | Custom HTTP Header | `x-codeium-csrf-token: <csrf_token>` on localhost endpoint |
| **Antigravity (Remote)** | Google OAuth 2.0 Access Token | HTTP Authorization Header | `Authorization: Bearer <accessToken>` for `cloudcode-pa.googleapis.com` |
| **Z.ai GLM** | Coding Plan API Key | HTTP Authorization Header | `Authorization: <apiKey>` *(Notice: without "Bearer " prefix)* |
| **Perplexity** | Browser Cookies (`cf_clearance`) | Embedded Chromium Session | In-process script `fetch` with `credentials: 'include'` in WebView2 context |

---

## 3. Session Lifecycle Management and Login Routes (`SignInRoute`)

When a credential is missing (`NeedsAuth`), expired, or revoked, the application cannot present a generic username/password form because each assistant manages its own proprietary authentication lifecycle.

TokenHound models this using the discriminated union `SignInRoute`:

```csharp
public abstract record SignInRoute
{
    // 1. Provider owns an in-app modal window managed by TokenHound (e.g. Perplexity WebView2)
    public sealed record Modal(string ProviderName) : SignInRoute;

    // 2. Launches the official tool installed on Windows that owns the session (e.g. Cursor, ChatGPT)
    public sealed record OpenApp(string ExecutableOrUri, string AppName) : SignInRoute;

    // 3. Terminal instructions for CLI-based tools (e.g. Claude Code)
    public sealed record Guidance(string Instructions) : SignInRoute;
}
```

### Provider Behavior on Windows 11:

1. **Claude Code**:
   - **Route**: `SignInRoute.Guidance("Run 'claude' in your terminal to sign in and refresh tokens. Use /login to switch accounts.")`.
   - **Reason**: Claude Code is a CLI tool without an official standalone GUI window.
2. **Cursor**:
   - **Route**: `SignInRoute.OpenApp("cursor://", "Cursor")`.
   - **Windows Action**: Launches Cursor via protocol `cursor://` or `%LOCALAPPDATA%\Programs\cursor\Cursor.exe`. The user authenticates in the editor UI, which saves tokens into SQLite.
3. **OpenAI Codex**:
   - **Route**: `SignInRoute.OpenApp("chatgpt://", "ChatGPT / Codex")`.
   - **Windows Action**: Launches the official ChatGPT desktop application on Windows 11.
4. **Antigravity**:
   - **Route**: `SignInRoute.OpenApp("antigravity://", "Antigravity")`.
   - **Windows Action**: Launches the Antigravity IDE to renew Google credentials.
5. **Z.ai GLM**:
   - **Route**: `SignInRoute.Guidance("Configure the Coding Plan API key in Claude Code's settings.json or ZCode/OpenCode configuration.")`.
6. **Perplexity**:
   - **Route**: `SignInRoute.Modal("Perplexity")`.
   - **Windows Action**: Opens an in-app native modal containing `WebView2` navigating to `https://www.perplexity.ai/`. The user logs in directly within the browser view.

---

## 4. Under the Hood: How Official Tools Authenticate

Reference documentation for how official tools execute their original logins:

### A. Claude Code (OAuth 2.0 with PKCE)
1. **Login Trigger**: On first run or `/login`, the CLI starts a temporary local HTTP server (e.g. `http://localhost:port/callback`) and generates a PKCE pair (`code_verifier` and `code_challenge`).
2. **Browser Navigation**: Opens the default browser to Anthropic's authorization page:
   `https://claude.ai/oauth/authorize?client_id=...&code_challenge=...&response_type=code`
3. **Authorization Code Exchange**: The browser redirects to `localhost:port` with the authorization code.
4. **Local Windows Storage**: Claude Code exchanges the code via POST at `https://api.anthropic.com/api/oauth/token` and writes credentials (`accessToken`, `refreshToken`, `expiresAt`) to `%USERPROFILE%\.claude\.credentials.json`.

### B. Cursor (WorkOS SSO / Web Session)
1. **Editor Authentication**: Cursor launches the browser to `https://cursor.com/login` via WorkOS.
2. **Protocol Callback**: Upon sign-in, the browser triggers custom protocol `cursor://` carrying the session token.
3. **Persistence**: The Cursor process updates SQLite at `%APPDATA%\Cursor\User\globalStorage\state.vscdb`, writing rows to `ItemTable`:
   - `cursorAuth/accessToken`: JWT authentication token.
   - `cursorAuth/stripeMembershipAuthId`: Account identifier (WorkOS ID).

### C. Google Antigravity (Google OAuth 2.0 via Go Keyring)
1. **Google Identity Flow**: Antigravity initiates the Google OAuth consent flow (`accounts.google.com/o/oauth2/v2/auth`) requesting Gemini and Cloud Code scopes.
2. **Windows Storage**: Via the Go `keyring` package (invoking `advapi32.dll`), the application calls `CredWriteW` creating a generic credential:
   - Target: `gemini:antigravity`.
   - Blob: JSON with `{"token": {"access_token": "ya29...", "expiry": "..."}, "auth_method": "consumer"}`.

### D. OpenAI Codex / ChatGPT Desktop
1. **Authentication**: ChatGPT Desktop authenticates via OAuth 2.0 with Auth0 / OpenAI (`auth0.openai.com` or `api.openai.com/oauth`).
2. **Persistence**: Saves JSON to `%USERPROFILE%\.codex\auth.json` containing `access_token`, `refresh_token`, and `id_token` (JWT with plan and profile metadata).
3. **Local Server**: The `codex.exe` binary loads this file internally when launching `app-server`, requiring zero external network authentication when queried via stdio JSON-RPC.

### E. Perplexity AI (Cloudflare Clearance + Session Cookies)
1. **Cloudflare Challenge**: Web navigation requires passing Cloudflare Turnstile / Bot Management checks.
2. **Session Cookies**: Sign-in generates `__Secure-next-auth.session-token` and `cf_clearance`.
3. **Windows Persistence**: The `WebView2` Chromium engine saves cookies to `%LOCALAPPDATA%\TokenHound\WebView2_Perplexity\EBWebView\Default\Network\Cookies`, encrypted on disk via Windows DPAPI.

---

## 5. Architectural Recommendations for C# (.NET)

1. **Prioritize *Borrow-Don't-Own***:
   - TokenHound reads existing credentials. Developer friction is zero: no passwords, no API tokens to create; installing and logging into Cursor or Claude Code makes TokenHound work immediately.
2. **Handling Expired Tokens**:
   - Never guess passwords or force refreshes on third-party credentials. If a token expires, mark status as `Stale` and provide the corresponding action button to launch the official tool.
3. **Optional Standalone OAuth PKCE**:
   - If the project ever decides to offer an independent standalone mode for Claude Code, a local C# `HttpListener` listening on `http://127.0.0.1:54123/callback` can receive Anthropic OAuth codes and store isolated credentials encrypted via DPAPI (`ProtectedData`).
