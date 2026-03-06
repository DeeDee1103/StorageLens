namespace StorageLens.Services.Orchestrator.Models;

public class WorkflowError
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
