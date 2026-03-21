namespace StorageLens.Services.Orchestrator.Models;

public enum WorkflowStepStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public static class WorkflowStepStateContract
{
    public static readonly IReadOnlySet<string> KnownStepNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Scanning",
        "Hashing",
        "DetectingDuplicates",
        "RefreshingAnalytics",
        "Workflow"
    };

    private static readonly IReadOnlyDictionary<WorkflowStepStatus, HashSet<WorkflowStepStatus>> AllowedTransitions =
        new Dictionary<WorkflowStepStatus, HashSet<WorkflowStepStatus>>
        {
            [WorkflowStepStatus.Pending] = [WorkflowStepStatus.Running, WorkflowStepStatus.Failed],
            [WorkflowStepStatus.Running] = [WorkflowStepStatus.Completed, WorkflowStepStatus.Failed],
            [WorkflowStepStatus.Completed] = [],
            [WorkflowStepStatus.Failed] = []
        };

    public static void EnsureKnownStepName(string stepName)
    {
        if (!KnownStepNames.Contains(stepName))
        {
            throw new InvalidOperationException($"Unknown workflow step '{stepName}'.");
        }
    }

    public static WorkflowStepStatus ParseStatus(string rawStatus)
    {
        if (!Enum.TryParse<WorkflowStepStatus>(rawStatus, true, out var status))
        {
            throw new InvalidOperationException($"Unknown workflow step status '{rawStatus}'.");
        }

        return status;
    }

    public static void EnsureTransitionAllowed(WorkflowStepStatus current, WorkflowStepStatus next)
    {
        if (current == next)
        {
            return;
        }

        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
        {
            throw new InvalidOperationException(
                $"Invalid workflow step status transition from '{current}' to '{next}'.");
        }
    }
}
