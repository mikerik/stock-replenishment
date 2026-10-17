using Microsoft.EntityFrameworkCore;
using StockReplenishment.Api;
using StockReplenishment.Data;
using StockReplenishment.Domain;
using StockReplenishment.StockCheck;

namespace StockReplenishment.Services;

/// <summary>Use cases: load the request, call the domain method, save, return a DTO.</summary>
public class RequestService(AppDbContext db, IStockCheckQueue stockCheckQueue)
{
    public async Task<PagedResult<RequestSummaryDto>> SearchAsync(
        RequestStatus? status, Priority? priority, int? locationId, int page, int pageSize)
    {
        var query = db.Requests.AsNoTracking();
        if (status is not null) query = query.Where(r => r.Status == status);
        if (priority is not null) query = query.Where(r => r.Priority == priority);
        if (locationId is not null) query = query.Where(r => r.LocationId == locationId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RequestSummaryDto(
                r.Id, r.Location!.Name, r.Priority, r.Status, r.StockCheckStatus, r.RequestedBy, r.Lines.Count, r.CreatedAt))
            .ToListAsync();

        return new PagedResult<RequestSummaryDto>(items, total, page, pageSize);
    }

    public async Task<RequestDto> GetAsync(int id) => ToDto(await LoadAsync(id));

    public async Task<RequestDto> CreateAsync(CreateRequestDto input)
    {
        if (!await db.Locations.AnyAsync(l => l.Id == input.LocationId))
        {
            throw new BusinessRuleException($"Location {input.LocationId} does not exist.");
        }

        var request = new ReplenishmentRequest
        {
            LocationId = input.LocationId,
            Priority = input.Priority,
            RequestedBy = input.RequestedBy.Trim(),
            Lines = input.Lines.Select(l => new RequestLine
            {
                ArticleNumber = l.ArticleNumber.Trim(),
                Description = l.Description.Trim(),
                RequestedQuantity = l.RequestedQuantity,
            }).ToList(),
        };

        db.Requests.Add(request);
        await db.SaveChangesAsync();
        return await GetAsync(request.Id);
    }

    public async Task<RequestDto> SubmitAsync(int id)
    {
        var request = await LoadAsync(id);
        request.Submit();
        await db.SaveChangesAsync();

        // Queue only after the Pending state is saved, so the worker never sees an unsaved request.
        await stockCheckQueue.EnqueueAsync(id);
        return ToDto(request);
    }

    public Task<RequestDto> ApproveAsync(int id) => UpdateAsync(id, r => r.Approve());

    public Task<RequestDto> RejectAsync(int id, string reason) => UpdateAsync(id, r => r.Reject(reason));

    public Task<RequestDto> FulfillAsync(int id, FulfillDto input) =>
        UpdateAsync(id, r => r.Fulfill(input.Lines
            .GroupBy(l => l.LineId)
            .ToDictionary(g => g.Key, g => g.First().FulfilledQuantity)));

    private async Task<RequestDto> UpdateAsync(int id, Action<ReplenishmentRequest> change)
    {
        var request = await LoadAsync(id);
        change(request);
        await db.SaveChangesAsync();
        return ToDto(request);
    }

    private async Task<ReplenishmentRequest> LoadAsync(int id) =>
        await db.Requests.Include(r => r.Location).Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == id)
        ?? throw new NotFoundException($"Request {id} was not found.");

    private static RequestDto ToDto(ReplenishmentRequest r) => new(
        r.Id, r.LocationId, r.Location?.Name ?? "", r.Priority, r.Status, r.StockCheckStatus, r.StockCheckMessage,
        r.RejectionReason, r.RequestedBy, r.CreatedAt, r.SubmittedAt,
        r.Lines.OrderBy(l => l.Id)
            .Select(l => new RequestLineDto(l.Id, l.ArticleNumber, l.Description, l.RequestedQuantity, l.AvailableQuantity, l.FulfilledQuantity))
            .ToList());
}