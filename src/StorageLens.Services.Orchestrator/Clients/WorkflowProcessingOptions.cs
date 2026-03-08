namespace StorageLens.Services.Orchestrator.Clients;

public class WorkflowProcessingOptions
{
    public const string SectionName = "WorkflowProcessing";

    public int MaxConcurrentWorkflows { get; set; } = 4;
}