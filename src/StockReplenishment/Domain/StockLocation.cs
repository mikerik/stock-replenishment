namespace StockReplenishment.Domain;

public class StockLocation
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}