using System.Net;
using System.Text;

namespace MegaFintradeRiskMonitor.Tests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes =
        new(StringComparer.OrdinalIgnoreCase);

    public List<HttpRequestMessage> Requests { get; } = new();

    public void AddJsonResponse(
        string path,
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _routes[path] = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public void AddTextResponse(
        string path,
        string text,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _routes[path] = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(text, Encoding.UTF8, "text/plain")
        };
    }

    public void AddFailure(
        string path,
        HttpStatusCode statusCode)
    {
        _routes[path] = _ => new HttpResponseMessage(statusCode);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (_routes.TryGetValue(path, out var responseFactory))
        {
            return Task.FromResult(responseFactory(request));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                $"No fake response configured for path '{path}'.",
                Encoding.UTF8,
                "text/plain")
        });
    }
}