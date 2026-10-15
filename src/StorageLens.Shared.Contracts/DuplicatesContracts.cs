namespace StorageLens.Shared.Contracts;

public record DuplicateGroupDto(
    Guid Id,
    string HashValue,
    int FileCount,
    long TotalSizeBytes,
    long WastedBytes,
    string ExampleFileName,
    IReadOnlyList<string> Locations);

public record DuplicateSummaryDto(
    int DuplicateGroups,
    int DuplicateFiles,
    long WastedBytes,
    double DuplicatePercent);

public record DuplicateRecalculationResultDto(
    Guid ScanJobId,
    string CorrelationId,
    int DuplicateGroups,
    int DuplicateFiles,
    long WastedBytes,
    DateTimeOffset RecalculatedAtUtc);
