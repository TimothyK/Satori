using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models
{
    public class WorkItem
    {
        public int id { get; set; }
        public int rev { get; set; }
        public Fields fields { get; set; }
        public string url { get; set; }
    }

    public class Fields
    {
        public string SystemAreaPath { get; set; }
        public string SystemTeamProject { get; set; }
        public string SystemIterationPath { get; set; }
        [JsonPropertyName("System.WorkItemType")]
        public string SystemWorkItemType { get; set; }
        public string SystemState { get; set; }
        public string SystemReason { get; set; }
        [JsonPropertyName("System.AssignedTo")]
        public User? SystemAssignedTo { get; set; }
        [JsonPropertyName("System.CreatedDate")] 
        public DateTimeOffset SystemCreatedDate { get; set; }
        [JsonPropertyName("System.CreatedBy")]
        public User SystemCreatedBy { get; set; }
        public DateTime SystemChangedDate { get; set; }
        public User SystemChangedBy { get; set; }
        public int SystemCommentCount { get; set; }
        [JsonPropertyName("System.Title")]
        public string SystemTitle { get; set; }
        public float MicrosoftVSTSSchedulingOriginalEstimate { get; set; }
        public DateTime MicrosoftVSTSCommonStateChangeDate { get; set; }
        public DateTime MicrosoftVSTSCommonActivatedDate { get; set; }
        public User MicrosoftVSTSCommonActivatedBy { get; set; }
        public DateTime MicrosoftVSTSCommonClosedDate { get; set; }
        public User MicrosoftVSTSCommonClosedBy { get; set; }
        public int MicrosoftVSTSCommonPriority { get; set; }
        public string SystemBoardColumn { get; set; }
        public bool SystemBoardColumnDone { get; set; }
        public string MicrosoftVSTSCommonSeverity { get; set; }
        public string MicrosoftVSTSCommonValueArea { get; set; }
        public string MicrosoftVSTSCommonTriage { get; set; }
        public string MicrosoftVSTSCMMIBlocked { get; set; }
        public float MicrosoftVSTSCommonBacklogPriority { get; set; }
        public string WEF_9C125D321B9B4525BE75E33CE4ACA209_KanbanColumn { get; set; }
        public bool WEF_9C125D321B9B4525BE75E33CE4ACA209_KanbanColumnDone { get; set; }
        public string MicrosoftVSTSTCMSystemInfo { get; set; }
        public string MicrosoftVSTSTCMReproSteps { get; set; }
        public string CustomObservedBehavior { get; set; }
    }

}
