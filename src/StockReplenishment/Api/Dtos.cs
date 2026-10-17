using StockReplenishment.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StockReplenishment.Api;

// ---- Responses ----

public record LocationDto(int Id, string Code, string Name);

public record RequestSummaryDto(
    int Id, string LocationName, Priority Priority, RequestStatus Status,
    StockCheckStatus StockCheckStatus, string RequestedBy, int LineCount, DateTime CreatedAt);

public record RequestLineDto(
    int Id, string ArticleNumber, string Description, int RequestedQuantity, int? AvailableQuantity, int? FulfilledQuantity);

public record RequestDto(
    int Id, int LocationId, string LocationName, Priority Priority, RequestStatus Status,
    StockCheckStatus StockCheckStatus, string? StockCheckMessage, string? RejectionReason,
    string RequestedBy, DateTime CreatedAt, DateTime? SubmittedAt, List<RequestLineDto> Lines);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

// ---- Requests (validated automatically by [ApiController]) ----

public class CreateRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a location.")]
    public int LocationId { get; set; }

    public Priority Priority { get; set; } = Priority.Normal;

    [Required, MaxLength(100)]
    public string RequestedBy { get; set; } = "";

    [MinLength(1, ErrorMessage = "Add at least one line.")]
    public List<CreateLineDto> Lines { get; set; } = [];
}

public class CreateLineDto
{
    [Required, MaxLength(32)]
    public string ArticleNumber { get; set; } = "";

    [Required, MaxLength(200)]
    public string Description { get; set; } = "";

    [Range(1, 100_000)]
    public int RequestedQuantity { get; set; } = 1;
}

public record RejectDto([Required] string Reason);

public record FulfillLineDto(int LineId, [Range(0, 100_000)] int FulfilledQuantity);

public record FulfillDto([MinLength(1)] List<FulfillLineDto> Lines);