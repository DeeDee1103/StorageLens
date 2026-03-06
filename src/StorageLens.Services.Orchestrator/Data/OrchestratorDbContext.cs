using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.Orchestrator.Data;

public class OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStepExecution> WorkflowStepExecutions => Set<WorkflowStepExecution>();
    public DbSet<WorkflowError> WorkflowErrors => Set<WorkflowError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.Orchestration);

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("WorkflowInstance");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RootPath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000).IsRequired();
            entity.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.WorkflowInstanceId);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.ScanJobId);
        });

        modelBuilder.Entity<WorkflowStepExecution>(entity =>
        {
            entity.ToTable("WorkflowStepExecution");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StepName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.WorkflowInstanceId);
        });

        modelBuilder.Entity<WorkflowError>(entity =>
        {
            entity.ToTable("WorkflowError");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StepName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.WorkflowInstanceId);
        });
    }
}
