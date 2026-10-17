using System;
using System.Collections.Generic;

namespace StockReplenishment.Domain;

/// <summary>
/// The workflow rules live here: every status change goes through a method that checks it is allowed.
/// </summary>
public class ReplenishmentRequest
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public StockLocation? Location { get; set; }
    public Priority Priority { get; set; }
    public required string RequestedBy { get; set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Draft;
    public StockCheckStatus StockCheckStatus { get; private set; } = StockCheckStatus.NotStarted;
    public string? StockCheckMessage { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; private set; }
    public List<RequestLine> Lines { get; set; } = [];

    public void Submit()
    {
        EnsureStatus(RequestStatus.Draft);
        if (Lines.Count == 0)
        {
            throw new BusinessRuleException("A request needs at least one line before it can be submitted.");
        }

        Status = RequestStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        StockCheckStatus = StockCheckStatus.Pending;
    }

    public void CompleteStockCheck(IReadOnlyDictionary<string, int> availableByArticle)
    {
        // The request may have been rejected while the slow check was running; then the result is ignored.
        if (Status != RequestStatus.Submitted || StockCheckStatus != StockCheckStatus.Pending)
        {
            return;
        }

        foreach (var line in Lines)
        {
            line.AvailableQuantity = availableByArticle.GetValueOrDefault(line.ArticleNumber);
        }

        var shortLines = Lines.Count(l => l.AvailableQuantity < l.RequestedQuantity);
        StockCheckStatus = StockCheckStatus.Completed;
        StockCheckMessage = shortLines == 0
            ? "All items are available."
            : $"{shortLines} of {Lines.Count} item(s) have insufficient stock.";
    }

    public void FailStockCheck(string reason)
    {
        if (StockCheckStatus == StockCheckStatus.Pending)
        {
            StockCheckStatus = StockCheckStatus.Failed;
            StockCheckMessage = reason;
        }
    }

    public void Approve()
    {
        EnsureStatus(RequestStatus.Submitted);
        if (StockCheckStatus == StockCheckStatus.Pending)
        {
            throw new InvalidStateException("Wait for the stock check to finish before approving.");
        }

        Status = RequestStatus.Approved;
    }

    public void Reject(string reason)
    {
        EnsureStatus(RequestStatus.Submitted);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleException("A rejection reason is required.");
        }

        Status = RequestStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    public void Fulfill(IReadOnlyDictionary<int, int> fulfilledByLineId)
    {
        EnsureStatus(RequestStatus.Approved);
        foreach (var line in Lines)
        {
            if (!fulfilledByLineId.TryGetValue(line.Id, out var quantity) || quantity < 0 || quantity > line.RequestedQuantity)
            {
                throw new BusinessRuleException(
                    $"Line {line.ArticleNumber} needs a fulfilled quantity between 0 and {line.RequestedQuantity}.");
            }
        }

        foreach (var line in Lines)
        {
            line.FulfilledQuantity = fulfilledByLineId[line.Id];
        }

        Status = RequestStatus.Fulfilled;
    }

    private void EnsureStatus(RequestStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidStateException($"The request is {Status}; this action requires {expected}.");
        }
    }
}