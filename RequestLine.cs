namespace StockReplenishment.Domain;

public class RequestLine
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public required string ArticleNumber { get; set; }
    public required string Description { get; set; }
    public int RequestedQuantity { get; set; }

    /// <summary>Set by the external stock check; null until it has completed.</summary>
    public int? AvailableQuantity { get; set; }

    public int? FulfilledQuantity { get; set; }
}