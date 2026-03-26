namespace StorageLens.Shared.Contracts;

public record DashboardKpiDto(
	long TotalStorageBytes,
	long TotalFiles,
	double DuplicatePercent,
	long PotentialSavingsBytes);

public record ChartPointDto(string Label, double Value);

public record DashboardDto(
	DashboardKpiDto Kpis,
	IReadOnlyList<ChartPointDto> StorageGrowth,
	IReadOnlyList<ChartPointDto> FileTypeDistribution,
	IReadOnlyList<ChartPointDto> LargestLocations,
	IReadOnlyList<ScanJobDto> RecentJobs,
	IReadOnlyList<DuplicateGroupDto> TopDuplicateGroups,
	IReadOnlyList<StorageLocationDto> Locations);

public record ReportsDto(
	IReadOnlyList<FileRecordDto> OldestFiles,
	IReadOnlyList<FileRecordDto> LargestFiles,
	IReadOnlyList<ChartPointDto> MostCommonFileTypes,
	IReadOnlyList<ChartPointDto> BiggestStorageConsumers,
	IReadOnlyList<ChartPointDto> DuplicateHeavyLocations);
