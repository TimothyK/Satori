using Satori.AzureDevOps.Models;
using System.Net;
using System.Text.Json;

namespace Satori.AzureDevOps.Exceptions;

internal class AzureHttpRequestException : HttpRequestException
{
    public static async Task<AzureHttpRequestException> FromResponseAsync(HttpResponseMessage response)
    {
        var fromUriMsg = response.RequestMessage == null ? string.Empty : $" from {response.RequestMessage.RequestUri}";
        if (response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Cannot generate request exception.  Response {fromUriMsg} successful.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var error = await JsonSerializer.DeserializeAsync<Error>(responseStream)
                    ?? throw new ApplicationException("Server did not respond");

        return new AzureHttpRequestException(error.message + fromUriMsg, response.StatusCode, error.typeKey); 
    }

    private AzureHttpRequestException(string message, HttpStatusCode statusCode, string typeKey, Exception? inner = null) 
        : base(message, inner, statusCode)
    {
        TypeKey = typeKey;
    }

    public string TypeKey { get; init; }
}