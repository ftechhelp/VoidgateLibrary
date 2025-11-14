using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VoidgateLibrary.Tests.Http;

internal class StubHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, Task<HttpResponseMessage>> Responder { get; set; } = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return CaptureAndRespondAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> CaptureAndRespondAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content != null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(ct);
        }
        return await Responder(request);
    }
}
