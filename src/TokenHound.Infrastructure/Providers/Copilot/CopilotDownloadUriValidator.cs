using System;
using System.Net;
using System.Net.Http;
using System.Security;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Validates signed download URLs to enforce HTTPS and block private or local destinations.
/// </summary>
internal static class CopilotDownloadUriValidator
{
    /// <summary>
    /// Validates that a download URI uses HTTPS and does not target local or private network destinations.
    /// </summary>
    /// <param name="uri">The URI to validate.</param>
    public static void Validate(Uri uri)
    {

        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps)
            throw new SecurityException("Signed download URL must use HTTPS protocol.");

        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            throw new SecurityException("Loopback destinations are not permitted for report downloads.");

        if (uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase))
            throw new SecurityException("Private network domains are not permitted for report downloads.");

        if (IPAddress.TryParse(uri.Host, out var ip) && (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || IsPrivateIpv4(ip)))
            throw new SecurityException("Private IP destinations are not permitted for report downloads.");
    }

    private static bool IsPrivateIpv4(IPAddress ip)
    {

        var bytes = ip.GetAddressBytes();

        if (bytes.Length != 4)
            return false;

        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || bytes[0] == 127
            || (bytes[0] == 169 && bytes[1] == 254)
            || bytes[0] == 0
            || bytes[0] >= 224;
    }

    /// <summary>
    /// Determines whether the specified HTTP status code represents a redirect.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns><c>true</c> if the status code is a redirect; otherwise, <c>false</c>.</returns>
    public static bool IsRedirectStatusCode(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or (HttpStatusCode)308;

    /// <summary>
    /// Resolves the redirection target URI against the current request URI.
    /// </summary>
    /// <param name="currentUri">The URI of the request that produced the redirect.</param>
    /// <param name="response">The HTTP response message containing the redirect.</param>
    /// <returns>The resolved absolute URI for the redirect target.</returns>
    public static Uri ResolveRedirectUri(Uri currentUri, HttpResponseMessage response)
    {

        var location = response.Headers.Location
            ?? throw new HttpRequestException("Redirect response missing Location header.");

        return location.IsAbsoluteUri ? location : new Uri(currentUri, location);
    }
}
