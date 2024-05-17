using Flurl;
using RichardSzalay.MockHttp;

namespace Satori.Kimai.Tests.Extensions;

public static class MockHttpExtensions
{
    public static MockedRequest WhenIsFullUrl(this MockHttpMessageHandler mockHttp, Url url)
    {
        return mockHttp
            .When(url)
            .With(request => request.RequestUri != null && request.RequestUri.Query == "?" + url.Query);
    }

}