using Satori.AzureDevOps.Models;
using System.Text.Json;

namespace Satori.AzureDevOps
{
    public class AzureDevOpsServer
    {
        private readonly string _token;

        public AzureDevOpsServer(string token)
        {
            _token = token;
        }

        public async Task<Value[]> GetPullRequestsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://devops.mayfield.pscl.com/PSDev/_apis/git/pullrequests/?api-version=6.0");

            request.Headers.Add("Authorization", $"Basic {_token}");

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
