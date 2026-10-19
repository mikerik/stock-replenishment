using StockReplenishment.Domain;

namespace StockReplenishment.Tests;

public class ReplenishmentRequestTests
{
    private static ReplenishmentRequest NewRequest() => new()
    {
        RequestedBy = "Anna",
        Lines = [new RequestLine { Id = 1, ArticleNumber = "100-2045", Description = "Bolt", RequestedQuantity = 10 }],
    };

    [Test]
    public void Submit_Draft_StartsStockCheck()
    {
        var request = NewRequest();

        request.Submit();

        Assert.That(request.Status, Is.EqualTo(RequestStatus.Submitted));
        Assert.That(request.StockCheckStatus, Is.EqualTo(StockCheckStatus.Pending));
    }

    [Test]
    public void Submit_WithoutLines_Throws()
    {
        var request = NewRequest();
        request.Lines.Clear();

        Assert.Throws<BusinessRuleException>(request.Submit);
    }

    [Test]
    public void Approve_WhileStockCheckPending_Throws()
    {
        var request = NewRequest();
        request.Submit();

        Assert.Throws<InvalidStateException>(request.Approve);
    }

    [Test]
    public void Approve_Draft_Throws()
    {
        Assert.Throws<InvalidStateException>(NewRequest().Approve);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Reject_WithoutReason_Throws(string reason)
    {
        var request = NewRequest();
        request.Submit();

        Assert.Throws<BusinessRuleException>(() => request.Reject(reason));
    }

    [Test]
    public void CompleteStockCheck_RecordsAvailabilityAndShortages()
    {
        var request = NewRequest();
        request.Submit();

        request.CompleteStockCheck(new Dictionary<string, int> { ["100-2045"] = 4 });

        Assert.That(request.StockCheckStatus, Is.EqualTo(StockCheckStatus.Completed));
        Assert.That(request.Lines[0].AvailableQuantity, Is.EqualTo(4));
        Assert.That(request.StockCheckMessage, Does.Contain("insufficient"));
    }

    [Test]
    public void CompleteStockCheck_AfterRejection_IsIgnored()
    {
        var request = NewRequest();
        request.Submit();
        request.Reject("Not needed");

        request.CompleteStockCheck(new Dictionary<string, int> { ["100-2045"] = 100 });

        Assert.That(request.Lines[0].AvailableQuantity, Is.Null);
    }

    [Test]
    public void Fulfill_ApprovedRequest_RecordsQuantities()
    {
        var request = NewRequest();
        request.Submit();
        request.CompleteStockCheck(new Dictionary<string, int> { ["100-2045"] = 100 });
        request.Approve();

        request.Fulfill(new Dictionary<int, int> { [1] = 8 });

        Assert.That(request.Status, Is.EqualTo(RequestStatus.Fulfilled));
        Assert.That(request.Lines[0].FulfilledQuantity, Is.EqualTo(8));
    }

    [Test]
    public void Fulfill_MoreThanRequested_Throws()
    {
        var request = NewRequest();
        request.Submit();
        request.CompleteStockCheck(new Dictionary<string, int>());
        request.Approve();

        Assert.Throws<BusinessRuleException>(() => request.Fulfill(new Dictionary<int, int> { [1] = 11 }));
    }
}