using System.Collections.Generic;
using System.Threading.Channels;
using F2Share.Domain.Events;
using F2Share.Domain.ValueObjects;

namespace F2Share.Application.Abstractions;

public interface IFileSystemEventStream : IAsyncDisposable
{
    ChannelReader<FileChangeDetected> Reader { get; }
    Task StartAsync(CancellationToken cancellationToken);
}

public interface IMetadataStore
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<FileFingerprint?> GetFingerprintAsync(string shareId, string relativePath, CancellationToken cancellationToken);
    Task UpsertFingerprintAsync(string shareId, FileFingerprint fingerprint, CancellationToken cancellationToken);
    Task MarkDeletedAsync(string shareId, string relativePath, DateTimeOffset atUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<FileFingerprint>> ListFingerprintsAsync(string shareId, CancellationToken cancellationToken);
}

public interface IHashProvider
{
    Task<string> ComputeStrongHashAsync(string absolutePath, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ComputeBlockHashesAsync(string absolutePath, int blockSizeBytes, CancellationToken cancellationToken);
}

public interface IChunker
{
    Task<IReadOnlyList<FileChunk>> BuildDeltaAsync(
        string absolutePath,
        IReadOnlyList<string> remoteBlockHashes,
        int blockSizeBytes,
        CancellationToken cancellationToken);
}

public interface IDiscoveryService
{
    IAsyncEnumerable<DiscoveredPeer> DiscoverAsync(CancellationToken cancellationToken);
}

public interface IPeerTransport : IAsyncDisposable
{
    Task StartAsync(int listenPort, CancellationToken cancellationToken);
    Task ConnectAsync(DiscoveredPeer peer, CancellationToken cancellationToken);
    Task SendAsync(string peerDeviceId, TransportEnvelope envelope, CancellationToken cancellationToken);
    IAsyncEnumerable<TransportEnvelope> ReadIncomingAsync(CancellationToken cancellationToken);
}

public interface ITransferScheduler
{
    ValueTask QueueAsync(TransferJob job, CancellationToken cancellationToken);
    IAsyncEnumerable<TransferJob> DequeueAsync(CancellationToken cancellationToken);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IPathFilter
{
    bool ShouldSync(string relativePath, bool isDirectory);
}

public readonly record struct DiscoveredPeer(string DeviceId, string DisplayName, string Host, int Port, bool SupportsQuic);

public sealed record ShareSyncOptions(
    IReadOnlyList<string> IncludeRoots,
    IReadOnlyList<string> IgnorePatterns,
    bool SyncHidden = false);

public readonly record struct TransferJob(
    string ShareId,
    string PeerDeviceId,
    string RelativePath,
    string AbsolutePath,
    IReadOnlyList<FileChunk> Chunks,
    bool IsDelete,
    bool IsDirectory);

public readonly record struct FileChunk(long Offset, int Length, string Hash, byte[]? Data);

public sealed record TransportEnvelope(string FromDeviceId, string ToDeviceId, string MessageType, byte[] Payload, DateTimeOffset CreatedAtUtc);
