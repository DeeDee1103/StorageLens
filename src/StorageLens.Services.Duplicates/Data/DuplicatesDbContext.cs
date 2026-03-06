using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Duplicates.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.Duplicates.Data;

public class DuplicatesDbContext(DbContextOptions<DuplicatesDbContext> options) : DbContext(options)
{
    public DbSet<DuplicateGroup> DuplicateGroups => Set<DuplicateGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.Duplicates);

        modelBuilder.Entity<DuplicateGroup>(entity =>
        {
            entity.ToTable("DuplicateGroups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.HashValue).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ExampleFileName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.LocationsCsv).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.ScanJobId, x.HashValue }).IsUnique();
            entity.HasIndex(x => x.CorrelationId);
        });
    }
}
