using Flurl;
using Satori.AzureDevOps.Models;
using System.Text.Json;

namespace Satori.AzureDevOps
{
    public class AzureDevOpsServer
    {
        private readonly ConnectionSettings _connectionSettings;

        public AzureDevOpsServer(ConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
        }

        public async Task<Value[]> GetPullRequestsAsync()
        {
            var url = _connectionSettings.Url
                .AppendPathSegment("_apis/git/pullrequests")
                .AppendQueryParam("api-version", "6.0");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("Authorization", $"Basic {_connectionSettings.PersonalAccessToken}");
            

            using var client = new HttpClient();
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Azure DevOps returned {(int)response.StatusCode} {response.StatusCode}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var root = await JsonSerializer.DeserializeAsync<Rootobject>(responseStream) 
                       ?? throw new ApplicationException("Server did not respond"); ;

            return root.value;
        }

    }
}
