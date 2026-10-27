using Apps.Memoq.Models.ProjectHistory;
using MQS.ServerProject;

namespace Apps.Memoq.Utils.ProjectHistory;

public static class DeliveryEventMapper
{
    public static DeliveryEventsResponse Map(Guid projectId,
        IEnumerable<DocumentHistoryItemInfo>? history, Guid? userId = null)
    {
        var events = (history ?? Array.Empty<DocumentHistoryItemInfo>())
            .Where(item => item.HistoryItemType == DocumentHistoryItemType.DocumentDeliver)
            .Where(item => !userId.HasValue || item.ModifierUserGuid == userId.Value)
            .OrderBy(item => item.TimeStamp)
            .ThenBy(item => item.DocumentOrDivisionGuid)
            .ThenBy(item => item.ModifierUserGuid)
            .Select(item => new DeliveryEvent
            {
                ProjectId = projectId.ToString(),
                DocumentId = item.DocumentOrDivisionGuid == Guid.Empty
                    ? null : item.DocumentOrDivisionGuid.ToString(),
                DocumentName = item.DocumentOrDivisionName,
                UserId = item.ModifierUserGuid == Guid.Empty
                    ? null : item.ModifierUserGuid.ToString(),
                DeliveredAt = item.TimeStamp
            }).ToList();

        return new DeliveryEventsResponse { Events = events, Count = events.Count };
    }
}
