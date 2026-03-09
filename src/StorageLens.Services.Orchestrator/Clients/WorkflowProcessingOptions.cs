namespace StorageLens.Services.Orchestrator.Clients;

public class WorkflowProcessingOptions
{
    public const string SectionName = "WorkflowProcessing";

    /// <summary>
    /// Maximum number of workflow jobs that may execute concurrently.
    /// Defaults to the number of logical processors on the host machine.
    /// </summary>
    public int MaxConcurrentWorkflows { get; set; } = Environment.ProcessorCount;
}