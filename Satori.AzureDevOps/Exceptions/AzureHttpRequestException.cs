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
        using var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();

        if (response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            return new AzureHttpRequestException($"Unexpected error {(int)response.StatusCode} {fromUriMsg}. {Environment.NewLine}{responseBody}", response.StatusCode, "");
        }

        try
        {
            var error = JsonSerializer.Deserialize<Error>(responseBody)
                            ?? throw new ApplicationException("Server did not respond");
            return new AzureHttpRequestException(error.Message + fromUriMsg, response.StatusCode, error.TypeKey);
        }
        catch (JsonException ex)
        {

            throw new ApplicationException("Could not deserialize error response."
                + Environment.NewLine + "Request:" + fromUriMsg
                + Environment.NewLine + "Response:" + responseBody
                , ex);
        }
    }

    private AzureHttpRequestException(string message, HttpStatusCode statusCode, string typeKey, Exception? inner = null) 
        : base(message, inner, statusCode)
    {
        TypeKey = typeKey;
    }

    public string TypeKey { get; init; }
}