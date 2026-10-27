using Apps.Memoq.Models.ProjectHistory;
using Apps.Memoq.Models.ServerProjects.Requests;
using Apps.Memoq.Utils.ProjectHistory;
using Apps.MemoQ;
using Apps.MemoQ.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using MQS.ServerProject;

namespace Apps.Memoq.Actions;

[ActionList("Project history")]
public class ProjectHistoryActions(InvocationContext invocationContext) : MemoqInvocable(invocationContext)
{
    [Action("Get project work-start events",
        Description = "Get recorded work starts with the next delivery time for the same document and user.")]
    public async Task<WorkStartEventsResponse> GetWorkStartEvents(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] WorkStartEventsRequest options)
    {
        var projectId = ParseNonEmptyGuid(project.ProjectGuid);
        var documentIds = options.DocumentIds?.Select(ParseNonEmptyGuid).Distinct().ToArray();
        Guid? userId = options.UserId is null ? null : ParseNonEmptyGuid(options.UserId);

        // memoQ uses null for all documents. Treat an omitted or empty list identically.
        var request = new DocumentHistoryRequest
        {
            DocumentGuids = documentIds is { Length: > 0 } ? documentIds : null!
        };
        var history = await ExecuteWithHandling(() =>
            ProjectService.Service.GetDocumentHistoryAsync(projectId, request));

        return WorkStartEventMapper.Map(projectId, history, userId);
    }

    [Action("Get project delivery events",
        Description = "Get recorded 'delivered' document history entries, with user IDs, documents and delivery timestamps.")]
    public async Task<DeliveryEventsResponse> GetDeliveryEvents(
        [ActionParameter] ProjectRequest project,
        [ActionParameter] DeliveryEventsRequest options)
    {
        var projectId = ParseNonEmptyGuid(project.ProjectGuid);
        var documentIds = options.DocumentIds?.Select(ParseNonEmptyGuid).Distinct().ToArray();
        Guid? userId = options.UserId is null ? null : ParseNonEmptyGuid(options.UserId);

        // memoQ uses null for all documents. Treat an omitted or empty list identically.
        var request = new DocumentHistoryRequest
        {
            DocumentGuids = documentIds is { Length: > 0 } ? documentIds : null!
        };
        var history = await ExecuteWithHandling(() =>
            ProjectService.Service.GetDocumentHistoryAsync(projectId, request));

        return DeliveryEventMapper.Map(projectId, history, userId);
    }

    private static Guid ParseNonEmptyGuid(string value)
    {
        var guid = GuidExtensions.ParseWithErrorHandling(value);
        if (guid == Guid.Empty)
            throw new PluginMisconfigurationException("Project, document and user IDs must not be empty GUIDs.");
        return guid;
    }
}
