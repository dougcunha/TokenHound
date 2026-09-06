# Provider Technical Specification: Perplexity (WebSession via WebView2)

This specification details the technical integration with **Perplexity AI** on **Windows 11**, illustrating the architectural strategy needed to handle Cloudflare Bot Management challenges and the domain modeling for endpoints that report strictly remaining counts.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `perplexity`.
- **Display Name**: `Perplexity`.
- **Fidelity**: `.official`.
- **Headline Metric**: Remaining Pro Searches (`remaining_pro`).

---

## 2. Cloudflare Bot Management Challenge

Consuming the Perplexity API directly through standard HTTP clients (.NET `HttpClient`, `curl`, or backend proxies) returns:

```http
HTTP/2 403 Forbidden
cf-mitigated: challenge
server: cloudflare
```

### Why Stealing Browser Cookies Fails
Even if session cookies are extracted directly from Google Chrome or Microsoft Edge (decrypting the local SQLite cookie store via DPAPI and AES-GCM), the request fails. The `cf_clearance` cookie issued by Cloudflare is cryptographically bound to the TLS fingerprint (JA3 / JA4) and HTTP/2 framing characteristics of the browser session that performed the challenge.

### The Architectural Solution: Dedicated WebView2
Rather than spoofing browsers, requests are executed by a **real, native browser engine**. On Windows 11 with C#, TokenHound embeds the native **Microsoft.Web.WebView2** control (powered by the Edge Chromium engine):
- The user authenticates once in a modal WebView2 dialog.
- Cloudflare Turnstile challenges are completed interactively by the user.
- WebView2 maintains an isolated persistent profile directory in `%LOCALAPPDATA%\TokenHound\WebView2_Perplexity`.
- Periodic telemetry fetches run via in-process same-origin `fetch` injected into the background WebView2 page context.

---

## 3. C# (.NET) Script Execution Implementation

WebView2 executes an authenticated async call using the session's native browser cookies:

```csharp
public async Task<string> FetchRateLimitsViaWebView2Async(CoreWebView2 webView)
{
    string script = """
    (async function() {
        try {
            const response = await fetch('/rest/rate-limit/all', {
                credentials: 'include',
                headers: { 'Accept': 'application/json' }
            });
            const text = await response.text();
            
            // Remove huge unnecessary connector list from payload
            let cleaned = text;
            try {
                const parsed = JSON.parse(text);
                delete parsed.sources;
                cleaned = JSON.stringify(parsed);
            } catch(e) {}

            return JSON.stringify({ status: response.status, body: cleaned });
        } catch (err) {
            return JSON.stringify({ status: 0, error: err.toString() });
        }
    })();
    """;

    string jsonResult = await webView.ExecuteScriptAsync(script);
    return jsonResult;
}
```

---

## 4. Endpoint Response Schema (`/rest/rate-limit/all`)

```json
{
  "free_queries": {
    "available": true,
    "remaining_detail": {
      "kind": "exact",
      "remaining": 10
    }
  },
  "remaining_pro": 2,
  "remaining_research": 0,
  "remaining_agentic_research": 0,
  "remaining_labs": 0,
  "model_specific_limits": {}
}
```

---

## 5. Data Modeling and Visual Rules

> **FUNDAMENTAL CHARACTERISTIC**: Perplexity reports **only what remains**. It does not provide the total plan denominator, nor does it expose when searches reset.
>
> **ZERO FAKE DATA RULE**:
> - Never invent a denominator to force a synthetic percentage.
> - The TokenHound ring renders the neutral base track with no filled progress arc (`usedFraction = null`).
> - The central badge displays the exact integer count remaining (e.g. `2`).
> - The tooltip displays raw remaining counts:
>   - `"2 left"` (Pro searches)
>   - `"0 left"` (Research)
>   - `"10 left"` (Free queries - only when `kind == "exact"`)
> - Progress bars and time countdowns ("resets in...") are omitted for this provider.

---

## 6. Signing Out in WebView2

To sign out the Perplexity session on Windows:

```csharp
public async Task SignOutAsync(CoreWebView2Profile profile)
{
    // Clear cookies, local storage, and cached assets strictly for the Perplexity profile
    await profile.ClearBrowsingDataAsync(
        CoreWebView2BrowsingDataKinds.Cookies | 
        CoreWebView2BrowsingDataKinds.LocalStorage | 
        CoreWebView2BrowsingDataKinds.IndexedDb
    );
}
```
