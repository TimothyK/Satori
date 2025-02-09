using Shouldly;
using System.Text.Json;

namespace Satori.Kimai.Tests.Extensions;

internal static class HttpRequestMessageExtensions
{
    public static string ReadRequestBody(this HttpRequestMessage request)
    {
        request.Content.ShouldNotBeNull();
        using var stream = request.Content.ReadAsStream();
        using var reader = new StreamReader(stream);
        var body = reader.ReadToEnd();

        if (request.Content.Headers.ContentType?.MediaType?.StartsWith("application/json") ?? false)
        {
            Console.WriteLine(PrettyJson(body));
        }
        else
        {
            Console.WriteLine(body); 
        }

        return body;
    }

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };
    private static string PrettyJson(string body)
    {
        var doc = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(doc, WriteIndented);
    }
}