using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StockReplenishment.Data;
using StockReplenishment.Domain;
using StockReplenishment.StockCheck;

namespace StockReplenishment.Tests;

public class StockCheckProcessorTests
{
    private AppDbContext _db = null!;
    private IStockAvailabilityService _stockService = null!;
    private StockCheckProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        // A fresh, uniquely named in-memory database per test.
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _stockService = Substitute.For<IStockAvailabilityService>();
        _processor = new StockCheckProcessor(_db, _stockService, NullLogger<StockCheckProcessor>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task ProcessAsync_ServiceAnswers_StoresAvailability()
    {
        var id = await AddSubmittedRequestAsync();
        _stockService.GetAvailabilityAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["100-2045"] = 50 });

        await _processor.ProcessAsync(id, CancellationToken.None);

        var request = await _db.Requests.Include(r => r.Lines).SingleAsync();
        Assert.That(request.StockCheckStatus, Is.EqualTo(StockCheckStatus.Completed));
        Assert.That(request.Lines[0].AvailableQuantity, Is.EqualTo(50));
    }

    [Test]
    public async Task ProcessAsync_ServiceFails_MarksCheckFailed()
    {
        var id = await AddSubmittedRequestAsync();
        _stockService.GetAvailabilityAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("down"));

        await _processor.ProcessAsync(id, CancellationToken.None);

        var request = await _db.Requests.SingleAsync();
        Assert.That(request.StockCheckStatus, Is.EqualTo(StockCheckStatus.Failed));
    }

    [Test]
    public async Task ProcessAsync_UnknownRequest_DoesNotCallService()
    {
        await _processor.ProcessAsync(999, CancellationToken.None);

        await _stockService.DidNotReceiveWithAnyArgs().GetAvailabilityAsync(default!, default);
    }

    private async Task<int> AddSubmittedRequestAsync()
    {
        var request = new ReplenishmentRequest
        {
            LocationId = 1,
            RequestedBy = "Anna",
            Lines = [new RequestLine { ArticleNumber = "100-2045", Description = "Bolt", RequestedQuantity = 10 }],
        };
        request.Submit();
        _db.Requests.Add(request);
        await _db.SaveChangesAsync();
        return request.Id;
    }
}