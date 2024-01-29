using ColabTime.ViewModels;
using ColabTime.ViewModels.PullRequests;

namespace ColabTime.Services
{
    public class AzureDevOpsServer
    {
        public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
        {
            return GetSamplePullRequests();
        }


        public IEnumerable<PullRequest> GetSamplePullRequests()
        {
            var timothy = new Person()
            {
                Id = "MAYFIELD\\Timothyk",
                DisplayName = "Timothy Klenke",
                AvatarUrl = "https://devops.mayfield.pscl.com/PSDev/_api/_common/identityImage?id=c00ef764-dc77-4b32-9a19-590db59f039b"
            };
            var alex = new Person()
            {
                Id = "MAYFIELD\\AlexG",
                DisplayName = "Alex Garand",
                AvatarUrl = "https://devops.mayfield.pscl.com/PSDev/_api/_common/identityImage?id=443db5de-3fbc-4be4-a1f2-8b492923b657"
            };
            var jonathan = new Person()
            {
                Id = "MAYFIELD\\JonathanE",
                DisplayName = "Jonathan Ettie",
                AvatarUrl = "https://devops.mayfield.pscl.com/PSDev/_api/_common/identityImage?id=428991e1-02ed-4a09-8a96-17c777f25646"
            };
            var barbara = new Person()
            {
                Id = "MAYFIELD\\barbaray",
                DisplayName = "Barbara Yue Yang",
                AvatarUrl = "https://devops.mayfield.pscl.com/PSDev/_api/_common/identityImage?id=200dc4de-d4b4-4c44-a380-510d3e5d29c5"
            };


            return new[]
            {
            new PullRequest
            {
                Id = 1010,
                Title = "Populate ShipToKey",
                RepositoryName = "DbScripts-Eagle",
                Project = "CD",
                Url = string.Format("https://devops.mayfield.pscl.com/PSDev/{0}/_git/{1}/pullrequest/{2}", "CD", "DbScripts-Eagle",
                    1010),
                Status = Status.Open,
                AutoComplete = true,
                CreationDate = new DateTimeOffset(2024, 1, 19, 23, 26, 41, TimeSpan.Zero),
                CreatedBy = timothy,
                Reviews = new List<Review>()
                {
                    new Review()
                    {
                        Id = new Guid("443db5de-3fbc-4be4-a1f2-8b492923b657"), IsRequired = true, Vote = ReviewVote.Rejected,
                        Reviewer = alex
                    },
                }
            },
            new PullRequest
            {
                Id = 1011,
                Title = "SFTP",
                RepositoryName = "StorageManager",
                Project = "Shared",
                Url = string.Format("https://devops.mayfield.pscl.com/PSDev/{0}/_git/{1}/pullrequest/{2}", "Shared", "StorageManager",
                    1011),
                Status = Status.Draft,
                AutoComplete = false,
                CreationDate = new DateTimeOffset(2024, 1, 21, 5, 45, 14, TimeSpan.Zero),
                CreatedBy = timothy,
                Reviews = new List<Review>()
            },
            new PullRequest
            {
                Id = 1009,
                Title = "D28773 - Instrument Import Require Addition Of Silo Destination",
                RepositoryName = "LDMS",
                Project = "CQ",
                Url = string.Format("https://devops.mayfield.pscl.com/PSDev/{0}/_git/{1}/pullrequest/{2}", "CQ", "LDMS", 1009),
                Status = Status.Open,
                AutoComplete = true,
                CreationDate = new DateTimeOffset(2024, 1, 19, 18, 03, 46, TimeSpan.Zero),
                CreatedBy = jonathan,
                Reviews = new List<Review>()
                {
                    new Review()
                    {
                        Id = new Guid("200dc4de-d4b4-4c44-a380-510d3e5d29c5"), IsRequired = true, Vote = ReviewVote.NoVote,
                        Reviewer = barbara
                    },
                    new Review()
                    {
                        Id = new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"), IsRequired = true, Vote = ReviewVote.Approved,
                        Reviewer = timothy
                    },
                }
            },
        };
        }

    }
}
