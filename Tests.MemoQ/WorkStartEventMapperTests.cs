using Apps.Memoq.Utils.ProjectHistory;
using MQS.ServerProject;

namespace Tests.MemoQ;

[TestClass]
public class WorkStartEventMapperTests
{
    [TestMethod]
    public void SelectsOnlyWorkStartsAndSortsByRecordedTime()
    {
        var project = Guid.NewGuid();
        var user = Guid.NewGuid();
        var document = Guid.NewGuid();
        var early = new DateTime(2026, 2, 2, 14, 52, 45, DateTimeKind.Unspecified);
        var result = WorkStartEventMapper.Map(project, new[]
        {
            Item(DocumentHistoryItemType.WorkStartedOnProject, early.AddMinutes(10), user, document),
            Item(DocumentHistoryItemType.ProjectLaunched, early.AddMinutes(-1), user, document),
            Item(DocumentHistoryItemType.WorkStartedOnProject, early, user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, early.AddMinutes(20), user, document)
        });

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(early, result.Events[0].StartedAt);
        Assert.AreEqual(DateTimeKind.Unspecified, result.Events[0].StartedAt.Kind);
        Assert.AreEqual((DateTime?)early.AddMinutes(20), result.Events[0].DeliveredAt);
        Assert.AreEqual((DateTime?)early.AddMinutes(20), result.Events[1].DeliveredAt);
        Assert.AreEqual(DateTimeKind.Unspecified, result.Events[0].DeliveredAt!.Value.Kind);
        Assert.AreEqual(project.ToString(), result.Events[0].ProjectId);
        Assert.AreEqual(document.ToString(), result.Events[0].DocumentId);
        Assert.AreEqual(user.ToString(), result.Events[0].UserId);
        Assert.AreEqual("Example.docx (spa-US)", result.Events[0].DocumentName);
    }

    [TestMethod]
    public void FiltersByUserWithoutCollapsingRepeatedEntries()
    {
        var user = Guid.NewGuid();
        var entry = Item(DocumentHistoryItemType.WorkStartedOnProject, DateTime.UtcNow, user, Guid.NewGuid());
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), new[]
        {
            entry, entry,
            Item(DocumentHistoryItemType.WorkStartedOnProject, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid())
        }, user);
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Events.All(item => item.UserId == user.ToString()));
    }

    [TestMethod]
    public void PreservesProjectLevelEventsWithNoDocumentOrUser()
    {
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.WorkStartedOnProject, DateTime.UtcNow, Guid.Empty, Guid.Empty)
        });
        Assert.AreEqual(1, result.Count);
        Assert.IsNull(result.Events[0].DocumentId);
        Assert.IsNull(result.Events[0].UserId);
    }

    [TestMethod]
    public void EmptyHistoryReturnsEmptyCollection()
    {
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), Array.Empty<DocumentHistoryItemInfo>());
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.Events.Count);
    }

    [TestMethod]
    public void PairsOnlyWithNextDeliveryForSameDocumentAndUser()
    {
        var user = Guid.NewGuid();
        var document = Guid.NewGuid();
        var start = new DateTime(2026, 1, 27, 6, 36, 56);
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddMinutes(30), user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddSeconds(-1), user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddSeconds(1), Guid.NewGuid(), document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddSeconds(2), user, Guid.NewGuid()),
            Item(DocumentHistoryItemType.DocumentReturn, start.AddSeconds(3), user, document),
            Item(DocumentHistoryItemType.GroupSourcingDocumentDeliver, start.AddSeconds(4), user, document),
            Item(DocumentHistoryItemType.WorkflowStatusChange, start.AddSeconds(5), user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddSeconds(91), user, document),
            Item(DocumentHistoryItemType.WorkStartedOnProject, start, user, document)
        }, user);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual((DateTime?)start.AddSeconds(91), result.Events[0].DeliveredAt);
    }

    [TestMethod]
    public void ReopenedWorkUsesTheDeliveryFollowingEachStart()
    {
        var user = Guid.NewGuid();
        var document = Guid.NewGuid();
        var start = new DateTime(2026, 1, 27, 6, 0, 0);
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.WorkStartedOnProject, start, user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddMinutes(5), user, document),
            Item(DocumentHistoryItemType.WorkStartedOnProject, start.AddMinutes(10), user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddMinutes(15), user, document),
            Item(DocumentHistoryItemType.WorkStartedOnProject, start.AddMinutes(20), user, document)
        });
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual((DateTime?)start.AddMinutes(5), result.Events[0].DeliveredAt);
        Assert.AreEqual((DateTime?)start.AddMinutes(15), result.Events[1].DeliveredAt);
        Assert.IsNull(result.Events[2].DeliveredAt);
    }

    [TestMethod]
    public void MissingIdsDoNotMatchAnonymousDeliveries()
    {
        var user = Guid.NewGuid();
        var document = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.WorkStartedOnProject, start, user, Guid.Empty),
            Item(DocumentHistoryItemType.WorkStartedOnProject, start, Guid.Empty, document),
            Item(DocumentHistoryItemType.WorkStartedOnProject, start, Guid.Empty, Guid.Empty),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddMinutes(1), user, Guid.Empty),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddMinutes(1), Guid.Empty, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start.AddMinutes(1), Guid.Empty, Guid.Empty)
        });
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Events.All(item => item.DeliveredAt is null));
    }

    [TestMethod]
    public void DeliveryAtSameRecordedTimestampCanMatch()
    {
        var user = Guid.NewGuid();
        var document = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var result = WorkStartEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.WorkStartedOnProject, start, user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, start, user, document)
        });
        Assert.AreEqual((DateTime?)start, result.Events[0].DeliveredAt);
    }

    private static DocumentHistoryItemInfo Item(DocumentHistoryItemType type, DateTime time, Guid user, Guid doc)
        => new()
        {
            HistoryItemType = type, TimeStamp = time, ModifierUserGuid = user,
            DocumentOrDivisionGuid = doc, DocumentOrDivisionName = "Example.docx (spa-US)"
        };
}
