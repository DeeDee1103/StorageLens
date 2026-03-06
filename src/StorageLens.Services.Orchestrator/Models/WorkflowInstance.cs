namespace StorageLens.Services.Orchestrator.Models;

public class WorkflowInstance
{
    public Guid Id { get; set; }
    public Guid StorageLocationId { get; set; }
    public Guid ScanJobId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string LastError { get; set; } = string.Empty;

    public List<WorkflowStepExecution> Steps { get; set; } = [];
}
