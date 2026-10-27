using Blackbird.Applications.Sdk.Common;

namespace Apps.Memoq.Models.ProjectHistory;

public class WorkStartEventsResponse
{
    [Display("Events")]
    public List<WorkStartEvent> Events { get; set; } = new();

    [Display("Event count")]
    public int Count { get; set; }
}

public class WorkStartEvent
{
    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    [Display("Document or slice ID")]
    public string? DocumentId { get; set; }

    [Display("Document or slice name")]
    public string? DocumentName { get; set; }

    [Display("User ID")]
    public string? UserId { get; set; }

    [Display("Started at (as returned by memoQ)")]
    public DateTime StartedAt { get; set; }

    [Display("Delivered time (as returned by memoQ)")]
    public DateTime? DeliveredAt { get; set; }

    [Display("Event type")]
    public string EventType { get; set; } = "WorkStartedOnProject";
}
