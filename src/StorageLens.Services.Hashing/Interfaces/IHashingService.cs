using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Hashing.Interfaces;

public interface IHashingService
{
    Task<HashingRunResultDto> RunAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
    Task<HashingRunResultDto?> GetExecutionAsync(Guid id, CancellationToken cancellationToken);
}
