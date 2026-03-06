namespace StorageLens.Services.Orchestrator.Models;

public class WorkflowStepExecution
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Notes { get; set; } = string.Empty;
}
