using Flurl;
using Satori.AzureDevOps.Models;
using System.Text.Json;

namespace Satori.AzureDevOps
{
    public class AzureDevOpsServer
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly HttpClient _httpClient;

        public AzureDevOpsServer(ConnectionSettings connectionSettings) : this(connectionSettings, new HttpClient())
        {
        }
        public AzureDevOpsServer(ConnectionSettings connectionSettings, HttpClient httpClient)
        {
            _connectionSettings = connectionSettings;
            _httpClient = httpClient;
        }

        public async Task<PullRequest[]> GetPullRequestsAsync()
        {
            var url = _connectionSettings.Url
                .AppendPathSegment("_apis/git/pullrequests")
                .AppendQueryParam("api-version", "6.0");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("Authorization", $"Basic {_connectionSettings.PersonalAccessToken}");
            
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Azure DevOps returned {(int)response.StatusCode} {response.StatusCode}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var root = await JsonSerializer.DeserializeAsync<RootObject<PullRequest>>(responseStream) 
                       ?? throw new ApplicationException("Server did not respond"); ;

            return root.value;
        }

    }
}
