using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// HTTP handler double that answers each request through a script and records every request it receives.
/// </summary>
internal sealed class ScriptedHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> script) : HttpMessageHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptedHttpHandler"/> class with a synchronous script.
    /// </summary>
    /// <param name="script">Produces the response for each request.</param>
    public ScriptedHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> script)
        : this(request => Task.FromResult(script(request)))
    {
    }

    /// <summary>Gets the requests received, in order.</summary>
    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {

        Requests.Enqueue(request);

        return script(request);
    }
}
