using Microsoft.EntityFrameworkCore;
using StockReplenishment.Domain;

namespace StockReplenishment.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<StockLocation> Locations => Set<StockLocation>();
    public DbSet<ReplenishmentRequest> Requests => Set<ReplenishmentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplenishmentRequest>(request =>
        {
            request.HasOne(r => r.Location).WithMany().HasForeignKey(r => r.LocationId);
            request.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.RequestId);
            request.Property(r => r.RequestedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<StockLocation>().HasIndex(l => l.Code).IsUnique();
    }
}