using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flurl;

namespace Satori.MessageQueues;

public class Publisher<T>(ConnectionSettings settings, HttpClient httpClient)
{
    public virtual Task SendAsync(T message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.Serialize(message);

        return SendMessageAsync(payload);
    }

    private async Task SendMessageAsync(string payload)
    {
        var requestUri = QueueUri.AppendPathSegment("messages");
        var sasToken = GetSasToken();

        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Authorization", sasToken);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        
        var response = await httpClient.SendAsync(request);
        await ThrowIfBadStatusCode(response);
    }

    private string GetSasToken()
    {
        var resourceUri = QueueUri;

        var expiry = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var stringToSign = $"{resourceUri}\n{expiry}";
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(settings.Key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var sasToken = $"SharedAccessSignature sr={resourceUri}&sig={Uri.EscapeDataString(signature)}&se={expiry}&skn={settings.KeyName}";
        return sasToken;
    }

    private Url QueueUri => 
        new Uri($"https://{settings.Subdomain}.servicebus.windows.net/")
            .AppendPathSegment(settings.QueueName);

    private static async Task ThrowIfBadStatusCode(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var fromUriMsg = response.RequestMessage == null ? string.Empty : $" from {response.RequestMessage.RequestUri}";
            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();

            throw new ApplicationException($"Unexpected error {(int)response.StatusCode} {fromUriMsg}. {Environment.NewLine}{responseBody}");
        }
    }
}