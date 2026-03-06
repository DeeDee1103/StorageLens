using System.Threading.Channels;

namespace StorageLens.Services.Orchestrator.Services;

public class WorkflowQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(workflowId, cancellationToken);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken);
}
