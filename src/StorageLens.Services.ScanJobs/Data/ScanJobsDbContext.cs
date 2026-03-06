using Microsoft.EntityFrameworkCore;
using StorageLens.Services.ScanJobs.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.ScanJobs.Data;

public class ScanJobsDbContext(DbContextOptions<ScanJobsDbContext> options) : DbContext(options)
{
    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.ScanJobs);

        modelBuilder.Entity<ScanJob>(entity =>
        {
            entity.ToTable("ScanJobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.LocationName).HasMaxLength(120);
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.HasIndex(x => x.StartTime);
        });
    }
}
