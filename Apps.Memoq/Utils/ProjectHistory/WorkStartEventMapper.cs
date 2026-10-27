using Apps.Memoq.Models.ProjectHistory;
using MQS.ServerProject;

namespace Apps.Memoq.Utils.ProjectHistory;

public static class WorkStartEventMapper
{
    public static WorkStartEventsResponse Map(Guid projectId,
        IEnumerable<DocumentHistoryItemInfo>? history, Guid? userId = null)
    {
        var entries = (history ?? Array.Empty<DocumentHistoryItemInfo>()).ToList();
        // Pair by stable document and user IDs, never by document name.
        // Missing IDs cannot establish a reliable match.
        var deliveries = entries
            .Where(item => item.HistoryItemType == DocumentHistoryItemType.DocumentDeliver)
            .Where(item => item.DocumentOrDivisionGuid != Guid.Empty && item.ModifierUserGuid != Guid.Empty)
            .OrderBy(item => item.TimeStamp)
            .ToLookup(item => (item.DocumentOrDivisionGuid, item.ModifierUserGuid));

        var events = entries
            .Where(item => item.HistoryItemType == DocumentHistoryItemType.WorkStartedOnProject)
            .Where(item => !userId.HasValue || item.ModifierUserGuid == userId.Value)
            .OrderBy(item => item.TimeStamp)
            .ThenBy(item => item.DocumentOrDivisionGuid)
            .ThenBy(item => item.ModifierUserGuid)
            .Select(item => new WorkStartEvent
            {
                ProjectId = projectId.ToString(),
                DocumentId = item.DocumentOrDivisionGuid == Guid.Empty
                    ? null : item.DocumentOrDivisionGuid.ToString(),
                DocumentName = item.DocumentOrDivisionName,
                UserId = item.ModifierUserGuid == Guid.Empty
                    ? null : item.ModifierUserGuid.ToString(),
                StartedAt = item.TimeStamp,
                // Nullable projection preserves no-match as null, not DateTime.MinValue.
                DeliveredAt = deliveries[(item.DocumentOrDivisionGuid, item.ModifierUserGuid)]
                    .Where(delivery => delivery.TimeStamp >= item.TimeStamp)
                    .Select(delivery => (DateTime?)delivery.TimeStamp)
                    .FirstOrDefault()
            }).ToList();

        return new WorkStartEventsResponse { Events = events, Count = events.Count };
    }
}
