using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Scanner.Interfaces;

public interface IScannerExecutionService
{
    Task<ScannerExecutionDto> ExecuteAsync(ScannerExecuteRequest request, CancellationToken cancellationToken);
    Task<ScannerExecutionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
