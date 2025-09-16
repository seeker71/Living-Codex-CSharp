using System.Net;
using System.Net.Http;

namespace LivingCodexMobile.Tests.Helpers;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }

    public static TestHttpMessageHandler FromResponse(HttpResponseMessage response)
        => new((_, __) => Task.FromResult(response));

    public static TestHttpMessageHandler Throws(Exception ex)
        => new((_, __) => Task.FromException<HttpResponseMessage>(ex));
}


