using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Locations.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.Locations.Data;

public class LocationsDbContext(DbContextOptions<LocationsDbContext> options) : DbContext(options)
{
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.Locations);

        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.ToTable("StorageLocations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.TotalSizeBytes);
        });
    }
}
