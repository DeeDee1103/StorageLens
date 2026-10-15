namespace StorageLens.Shared.Contracts;

public record HashingRunResultDto(
    Guid Id,
    Guid ScanJobId,
    string CorrelationId,
    int Attempted,
    int Succeeded,
    int Failed,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
