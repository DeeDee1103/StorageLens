using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Hashing.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.Hashing.Data;

public class HashingDbContext(DbContextOptions<HashingDbContext> options) : DbContext(options)
{
    public DbSet<HashExecution> HashExecutions => Set<HashExecution>();
    public DbSet<HashFailure> HashFailures => Set<HashFailure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.Hashing);

        modelBuilder.Entity<HashExecution>(entity =>
        {
            entity.ToTable("HashExecution");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.ScanJobId);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<HashFailure>(entity =>
        {
            entity.ToTable("HashFailure");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FilePath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.Error).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.HashExecutionId);
        });
    }
}
