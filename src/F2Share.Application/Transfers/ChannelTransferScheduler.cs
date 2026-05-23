using System.Threading.Channels;
using F2Share.Application.Abstractions;

namespace F2Share.Application.Transfers;

public sealed class ChannelTransferScheduler : ITransferScheduler
{
    private readonly Channel<TransferJob> _channel = Channel.CreateUnbounded<TransferJob>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    public ValueTask QueueAsync(TransferJob job, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    public async IAsyncEnumerable<TransferJob> DequeueAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }
}
