using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.FileInventory.Data;

public class FileInventoryDbContext(DbContextOptions<FileInventoryDbContext> options) : DbContext(options)
{
    public DbSet<FileRecord> FileRecords => Set<FileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.FileInventory);

        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.ToTable("FileRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.FilePath).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Extension).HasMaxLength(20).IsRequired();
            entity.Property(x => x.LocationName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.HashValue).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.StorageLocationId);
            entity.HasIndex(x => x.Extension);
            entity.HasIndex(x => x.FileName);
        });
    }
}
