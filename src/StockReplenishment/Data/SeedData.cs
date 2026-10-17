using StockReplenishment.Domain;
using System;

namespace StockReplenishment.Data;

public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        if (db.Locations.Any())
        {
            return;
        }

        var locations = new[]
        {
            new StockLocation { Code = "ASM-01", Name = "Assembly Line 1" },
            new StockLocation { Code = "ASM-02", Name = "Assembly Line 2" },
            new StockLocation { Code = "WLD-03", Name = "Welding Cell 3" },
            new StockLocation { Code = "PKG-01", Name = "Packaging" },
        };
        db.Locations.AddRange(locations);

        var articles = new (string Number, string Description)[]
        {
            ("100-2045", "Hex bolt M8x30"), ("100-2046", "Hex nut M8"), ("220-1450", "Wire harness WH-450"),
            ("310-0007", "Welding wire 1.0 mm"), ("510-0042", "Corrugated box 600x400"), ("820-0001", "Nitrile gloves L"),
        };
        string[] workers = ["Anna Lindqvist", "Johan Berg", "Maria Nilsson"];

        var random = new Random(42);
        for (var i = 0; i < 18; i++)
        {
            var request = new ReplenishmentRequest
            {
                Location = locations[i % locations.Length],
                Priority = (Priority)(i % 3),
                RequestedBy = workers[i % workers.Length],
                CreatedAt = DateTime.UtcNow.AddHours(-i * 7),
                Lines = articles.OrderBy(_ => random.Next()).Take(random.Next(1, 4))
                    .Select((a, index) => new RequestLine
                    {
                        Id = i * 10 + index + 1,
                        ArticleNumber = a.Number,
                        Description = a.Description,
                        RequestedQuantity = random.Next(1, 20) * 10,
                    })
                    .ToList(),
            };

            // Walk each request through the real workflow so every state is represented.
            var target = i % 6;
            if (target >= 1)
            {
                request.Submit();
                request.CompleteStockCheck(request.Lines.ToDictionary(l => l.ArticleNumber, l => l.RequestedQuantity * (i % 4 == 0 ? 0 : 3)));
            }

            if (target == 2) request.Reject("Duplicate of an open request.");
            if (target >= 3) request.Approve();
            if (target >= 4) request.Fulfill(request.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity));

            db.Requests.Add(request);
        }

        db.SaveChanges();
    }
}