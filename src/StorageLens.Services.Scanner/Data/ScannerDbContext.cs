using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Scanner.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.Scanner.Data;

public class ScannerDbContext(DbContextOptions<ScannerDbContext> options) : DbContext(options)
{
    public DbSet<ScanExecution> ScanExecutions => Set<ScanExecution>();
    public DbSet<ScanError> ScanErrors => Set<ScanError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.Scanner);

        modelBuilder.Entity<ScanExecution>(entity =>
        {
            entity.ToTable("ScanExecution");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RootPath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(700).IsRequired();
            entity.HasIndex(x => x.ScanJobId);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<ScanError>(entity =>
        {
            entity.ToTable("ScanError");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Path).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.ErrorType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.ScanExecutionId);
        });
    }
}
