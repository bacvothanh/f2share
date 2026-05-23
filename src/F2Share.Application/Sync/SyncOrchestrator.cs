using F2Share.Application.Abstractions;
using F2Share.Application.Transfers;
using F2Share.Domain.Events;
using Microsoft.Extensions.Logging;

namespace F2Share.Application.Sync;

public sealed class SyncOrchestrator
{
    private const int DefaultBlockSizeBytes = 256 * 1024;

    private readonly IFileSystemEventStream _eventStream;
    private readonly IMetadataStore _metadataStore;
    private readonly IHashProvider _hashProvider;
    private readonly DeltaPlanner _deltaPlanner;
    private readonly ITransferScheduler _transferScheduler;
    private readonly ILogger<SyncOrchestrator> _logger;
    private readonly ChangeDebouncer _debouncer;

    public SyncOrchestrator(
        IFileSystemEventStream eventStream,
        IMetadataStore metadataStore,
        IHashProvider hashProvider,
        DeltaPlanner deltaPlanner,
        ITransferScheduler transferScheduler,
        ILogger<SyncOrchestrator> logger)
    {
        _eventStream = eventStream;
        _metadataStore = metadataStore;
        _hashProvider = hashProvider;
        _deltaPlanner = deltaPlanner;
        _transferScheduler = transferScheduler;
        _logger = logger;
        _debouncer = new ChangeDebouncer(TimeSpan.FromMilliseconds(350));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _metadataStore.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _eventStream.StartAsync(cancellationToken).ConfigureAwait(false);

        while (await _eventStream.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_eventStream.Reader.TryRead(out var evt))
            {
                if (!_debouncer.TryAdd(evt))
                {
                    continue;
                }

                await ProcessEventAsync(evt, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessEventAsync(FileChangeDetected evt, CancellationToken cancellationToken)
    {
        if (evt.ChangeKind is FileChangeKind.Deleted or FileChangeKind.DirectoryDeleted)
        {
            await _metadataStore.MarkDeletedAsync(evt.ShareId, evt.RelativePath, evt.ObservedAtUtc, cancellationToken).ConfigureAwait(false);
            await _transferScheduler.QueueAsync(new TransferJob(evt.ShareId, "*", evt.RelativePath, string.Empty, [], true, evt.ChangeKind == FileChangeKind.DirectoryDeleted), cancellationToken).ConfigureAwait(false);
            return;
        }

        if (evt.ChangeKind is FileChangeKind.DirectoryCreated or FileChangeKind.DirectoryRenamed)
        {
            await _transferScheduler.QueueAsync(new TransferJob(evt.ShareId, "*", evt.RelativePath, Path.Combine(evt.RootPath, evt.RelativePath), [], false, true), cancellationToken).ConfigureAwait(false);
            return;
        }

        var absolutePath = Path.Combine(evt.RootPath, evt.RelativePath);
        if (!File.Exists(absolutePath))
        {
            return;
        }

        var strongHash = await _hashProvider.ComputeStrongHashAsync(absolutePath, cancellationToken).ConfigureAwait(false);
        var blockHashes = await _hashProvider.ComputeBlockHashesAsync(absolutePath, DefaultBlockSizeBytes, cancellationToken).ConfigureAwait(false);

        await _metadataStore.UpsertFingerprintAsync(
            evt.ShareId,
            new Domain.ValueObjects.FileFingerprint(evt.RelativePath, new FileInfo(absolutePath).Length, evt.ObservedAtUtc, strongHash, blockHashes),
            cancellationToken).ConfigureAwait(false);

        var chunks = await _deltaPlanner.PlanAsync(absolutePath, [], DefaultBlockSizeBytes, cancellationToken).ConfigureAwait(false);
        await _transferScheduler.QueueAsync(new TransferJob(evt.ShareId, "*", evt.RelativePath, absolutePath, chunks, false, false), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Scheduled sync for {Path} ({ChunkCount} chunk(s))", evt.RelativePath, chunks.Count);
    }
}
