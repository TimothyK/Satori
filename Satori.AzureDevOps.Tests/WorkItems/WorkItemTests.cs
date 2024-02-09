using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.WorkItems
{
    [TestClass]
    public class WorkItemTests
    {
        #region Helpers

        #region Arrange

        private const int WorkItemId = 2;

        private readonly ConnectionSettings _connectionSettings = new()
        {
            Url = new Uri("http://devops.test/Team"),
            PersonalAccessToken = "test"
        };

        private Url GetWorkItemUrl =>
            _connectionSettings.Url
                .AppendPathSegment("_apis/wit/workItems")
                .AppendQueryParam("ids", WorkItemId)
                .AppendQueryParam("api-version", "6.0");

        private readonly MockHttpMessageHandler _mockHttp = new();

        private void SetResponse(Url url, byte[] response)
        {
            _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
        }

        #endregion Arrange

        #region Act

        private WorkItem[] GetWorkItems()
        {
            var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient());
            return srv.GetWorkItemsAsync(WorkItemId).Result;
        }

        private WorkItem SingleWorkItem()
        {
            //Arrange
            SetResponse(GetWorkItemUrl, WorkItemResponses.SingleWorkItem);

            //Act
            var workItems = GetWorkItems();

            //Assert
            workItems.Length.ShouldBe(1);
            return workItems.Single();
        }

        #endregion Act

        #endregion Helpers

        [TestMethod]
        public void _SmokeTest() => SingleWorkItem().id.ShouldBe(WorkItemId);

        [TestMethod]
        public void Title() => SingleWorkItem().fields.SystemTitle.ShouldBe("Program no longer crashes on startup");



    }
}
