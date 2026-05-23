using F2Share.Application.Abstractions;

namespace F2Share.Application.Transfers;

public sealed class DeltaPlanner
{
    private readonly IChunker _chunker;

    public DeltaPlanner(IChunker chunker)
    {
        _chunker = chunker;
    }

    public Task<IReadOnlyList<FileChunk>> PlanAsync(
        string absolutePath,
        IReadOnlyList<string> remoteBlockHashes,
        int blockSizeBytes,
        CancellationToken cancellationToken)
    {
        return _chunker.BuildDeltaAsync(absolutePath, remoteBlockHashes, blockSizeBytes, cancellationToken);
    }
}
