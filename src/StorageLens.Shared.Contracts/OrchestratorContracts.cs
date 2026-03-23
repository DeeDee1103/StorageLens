namespace StorageLens.Shared.Contracts;

public enum WorkflowStatus
{
	Pending = 1,
	Running = 2,
	Completed = 3,
	Failed = 4,
	RetryQueued = 5,
	Cancelled = 6
}

public record WorkflowStepExecutionDto(
	string StepName,
	string Status,
	DateTimeOffset StartedAtUtc,
	DateTimeOffset? CompletedAtUtc,
	string Notes);

public record WorkflowInstanceDto(
	Guid Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string RootPath,
	string Status,
	DateTimeOffset CreatedAtUtc,
	DateTimeOffset? CompletedAtUtc,
	int RetryCount,
	string LastError,
	IReadOnlyList<WorkflowStepExecutionDto> Steps);

public record WorkflowStartResultDto(Guid WorkflowId, Guid ScanJobId, string CorrelationId, string Status);
