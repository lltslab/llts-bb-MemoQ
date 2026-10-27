using Apps.Memoq.Utils.ProjectHistory;
using MQS.ServerProject;

namespace Tests.MemoQ;

[TestClass]
public class DeliveryEventMapperTests
{
    [TestMethod]
    public void SelectsOnlyDeliveriesAndSortsByRecordedTime()
    {
        var project = Guid.NewGuid();
        var user = Guid.NewGuid();
        var document = Guid.NewGuid();
        var early = new DateTime(2026, 2, 2, 14, 52, 45, DateTimeKind.Unspecified);
        var result = DeliveryEventMapper.Map(project, new[]
        {
            Item(DocumentHistoryItemType.DocumentDeliver, early.AddMinutes(10), user, document),
            Item(DocumentHistoryItemType.ProjectLaunched, early.AddMinutes(-1), user, document),
            Item(DocumentHistoryItemType.DocumentDeliver, early, user, document),
            Item(DocumentHistoryItemType.WorkStartedOnProject, early.AddMinutes(20), user, document),
            Item(DocumentHistoryItemType.DocumentReturn, early.AddMinutes(21), user, document),
            Item(DocumentHistoryItemType.WorkflowStatusChange, early.AddMinutes(22), user, document),
            Item(DocumentHistoryItemType.GroupSourcingDocumentDeliver, early.AddMinutes(23), user, document)
        });

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(early, result.Events[0].DeliveredAt);
        Assert.AreEqual(early.AddMinutes(10), result.Events[1].DeliveredAt);
        Assert.AreEqual("DocumentDeliver", result.Events[0].EventType);
        Assert.AreEqual(DateTimeKind.Unspecified, result.Events[0].DeliveredAt.Kind);
        Assert.AreEqual(project.ToString(), result.Events[0].ProjectId);
        Assert.AreEqual(document.ToString(), result.Events[0].DocumentId);
        Assert.AreEqual(user.ToString(), result.Events[0].UserId);
        Assert.AreEqual("Example.docx (spa-US)", result.Events[0].DocumentName);
    }

    [TestMethod]
    public void FiltersByUserWithoutCollapsingRepeatedEntries()
    {
        var user = Guid.NewGuid();
        var entry = Item(DocumentHistoryItemType.DocumentDeliver, DateTime.UtcNow, user, Guid.NewGuid());
        var result = DeliveryEventMapper.Map(Guid.NewGuid(), new[]
        {
            entry, entry,
            Item(DocumentHistoryItemType.DocumentDeliver, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid())
        }, user);
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Events.All(item => item.UserId == user.ToString()));
    }

    [TestMethod]
    public void PreservesProjectLevelEventsWithNoDocumentOrUser()
    {
        var result = DeliveryEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.DocumentDeliver, DateTime.UtcNow, Guid.Empty, Guid.Empty)
        });
        Assert.AreEqual(1, result.Count);
        Assert.IsNull(result.Events[0].DocumentId);
        Assert.IsNull(result.Events[0].UserId);
    }

    [TestMethod]
    public void EmptyHistoryReturnsEmptyCollection()
    {
        var result = DeliveryEventMapper.Map(Guid.NewGuid(), Array.Empty<DocumentHistoryItemInfo>());
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.Events.Count);
    }

    [TestMethod]
    public void NullHistoryReturnsEmptyCollection()
    {
        var result = DeliveryEventMapper.Map(Guid.NewGuid(), null);
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.Events.Count);
    }

    [TestMethod]
    public void HistoryWithoutDeliveryReturnsEmptyCollection()
    {
        var result = DeliveryEventMapper.Map(Guid.NewGuid(), new[]
        {
            Item(DocumentHistoryItemType.WorkStartedOnProject, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid()),
            Item(DocumentHistoryItemType.DocumentReturn, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid())
        });
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.Events.Count);
    }

    private static DocumentHistoryItemInfo Item(DocumentHistoryItemType type, DateTime time, Guid user, Guid doc)
        => new()
        {
            HistoryItemType = type, TimeStamp = time, ModifierUserGuid = user,
            DocumentOrDivisionGuid = doc, DocumentOrDivisionName = "Example.docx (spa-US)"
        };
}
