using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StockReplenishment.Domain;
using StockReplenishment.Services;

namespace StockReplenishment.Api;

[ApiController]
[Route("api/requests")]
public class RequestsController(RequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<RequestSummaryDto>> Search(
        RequestStatus? status, Priority? priority, int? locationId,
        [Range(1, 10_000)] int page = 1, [Range(1, 100)] int pageSize = 20) =>
        await service.SearchAsync(status, priority, locationId, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<RequestDto> Get(int id) => await service.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(CreateRequestDto input)
    {
        var created = await service.CreateAsync(input);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Returns 202 Accepted: the stock check continues in the background. Poll GET to see the result.</summary>
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<RequestDto>> Submit(int id) =>
        AcceptedAtAction(nameof(Get), new { id }, await service.SubmitAsync(id));

    [HttpPost("{id:int}/approve")]
    public async Task<RequestDto> Approve(int id) => await service.ApproveAsync(id);

    [HttpPost("{id:int}/reject")]
    public async Task<RequestDto> Reject(int id, RejectDto input) => await service.RejectAsync(id, input.Reason);

    [HttpPost("{id:int}/fulfill")]
    public async Task<RequestDto> Fulfill(int id, FulfillDto input) => await service.FulfillAsync(id, input);
}

[ApiController]
[Route("api/locations")]
public class LocationsController(Data.AppDbContext db) : ControllerBase
{
    [HttpGet]
    public List<LocationDto> GetAll() =>
        db.Locations.OrderBy(l => l.Code).Select(l => new LocationDto(l.Id, l.Code, l.Name)).ToList();
}