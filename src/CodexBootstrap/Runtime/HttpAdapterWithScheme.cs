using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class HttpAdapterWithScheme : HttpAdapter
{
    private readonly string _scheme;
    public HttpAdapterWithScheme(string scheme) { _scheme = scheme; }
    public override string Scheme => _scheme;
}
