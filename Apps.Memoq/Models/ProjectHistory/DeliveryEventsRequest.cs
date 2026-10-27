using Blackbird.Applications.Sdk.Common;

namespace Apps.Memoq.Models.ProjectHistory;

public class DeliveryEventsRequest
{
    [Display("Document or slice IDs")]
    public IEnumerable<string>? DocumentIds { get; set; }

    [Display("User ID")]
    public string? UserId { get; set; }
}
