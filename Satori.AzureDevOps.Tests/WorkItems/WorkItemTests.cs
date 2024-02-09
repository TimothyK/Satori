﻿using Flurl;
using Pscl.CommaSeparatedValues;
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

        private readonly ConnectionSettings _connectionSettings = new()
        {
            Url = new Uri("http://devops.test/Team"),
            PersonalAccessToken = "test"
        };

        private Url GetWorkItemUrl(params int[] workItemIds) =>
            _connectionSettings.Url
                .AppendPathSegment("_apis/wit/workItems")
                .AppendQueryParam("ids", workItemIds.ToCommaSeparatedValues())
                .AppendQueryParam("api-version", "6.0");

        private readonly MockHttpMessageHandler _mockHttp = new();

        private void SetResponse(Url url, byte[] response)
        {
            _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
        }

        #endregion Arrange

        #region Act

        private WorkItem[] GetWorkItems(int workItemId)
        {
            var srv = new AzureDevOpsServer(_connectionSettings, _mockHttp.ToHttpClient());
            return srv.GetWorkItemsAsync(workItemId).Result;
        }

        private WorkItem SingleWorkItem(int workItemId = SingleWorkItemId)
        {
            //Arrange
            SetResponse(GetWorkItemUrl(workItemId), GetPayload(workItemId));

            //Act
            var workItems = GetWorkItems(workItemId);

            //Assert
            workItems.Length.ShouldBe(1);
            return workItems.Single();
        }

        private const int SingleWorkItemId = 2;
        private const int BlockedWorkItemId = 3;

        private static byte[] GetPayload(int workItemId)
        {
            if (workItemId == BlockedWorkItemId)
            {
                return WorkItemResponses.BlockedWorkItem;
            }
            return WorkItemResponses.SingleWorkItem;
        }

        #endregion Act

        #endregion Helpers

        [TestMethod]
        public void _SmokeTest() => SingleWorkItem().id.ShouldBe(SingleWorkItemId);

        [TestMethod]
        public void Title() => SingleWorkItem().fields.SystemTitle.ShouldBe("Program no longer crashes on startup");

        [TestMethod]
        public void WorkItemType() => SingleWorkItem().fields.WorkItemType.ShouldBe("Product Backlog Item");


        [TestMethod]
        public void Area() => SingleWorkItem().fields.AreaPath.ShouldBe("Product\\AppArea");

        [TestMethod]
        public void IterationPath() => SingleWorkItem().fields.IterationPath.ShouldBe("CD\\Skunk\\Sprint 2024-02");

        [TestMethod]
        public void State() => SingleWorkItem().fields.State.ShouldBe("New");

        [TestMethod]
        public void AssignedTo()
        {
            var assignedTo = SingleWorkItem().fields.AssignedTo;
            assignedTo.ShouldNotBeNull();
            assignedTo.id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
            assignedTo.displayName.ShouldBe("Timothy Klenke");
            assignedTo.imageUrl.ShouldBe("http://devops.test/Team/_apis/GraphProfile/MemberAvatars/win.Uy0xLTUtMjEtMTUyNzAwNjgzMS04OTQzOTEwNDQtNjIyNjExMjE2LTExNjc");
        }
        
        [TestMethod]
        public void CreatedBy()
        {
            var createdBy = SingleWorkItem().fields.CreatedBy;
            createdBy.ShouldNotBeNull();
            createdBy.id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
            createdBy.displayName.ShouldBe("Timothy Klenke");
            createdBy.imageUrl.ShouldBe("http://devops.test/Team/_apis/GraphProfile/MemberAvatars/win.Uy0xLTUtMjEtMTUyNzAwNjgzMS04OTQzOTEwNDQtNjIyNjExMjE2LTExNjc");
        }

        [TestMethod]
        public void CreationDate() => SingleWorkItem().fields.SystemCreatedDate
            .ShouldBe(new DateTimeOffset(2024, 1, 13, 20, 16, 53, TimeSpan.Zero).AddMilliseconds(407));


        [TestMethod]
        public void Priority() => SingleWorkItem().fields.Priority.ShouldBe(2);

        [TestMethod]
        public void BacklogPriority() => SingleWorkItem().fields.BacklogPriority.ShouldBe(27103990.0);

        [TestMethod]
        public void Blocked_No() => SingleWorkItem().fields.Blocked.ShouldBeFalse();
        
        [TestMethod]
        public void Blocked_Yes() => SingleWorkItem(BlockedWorkItemId).fields.Blocked.ShouldBeTrue();

        [TestMethod]
        public void ProjectCode() => SingleWorkItem().fields.ProjectCode.ShouldBe("1.2.3 - Skunk Works");
        
        [TestMethod]
        public void CommentCount() => SingleWorkItem().fields.CommentCount.ShouldBe(0);

    }
}
