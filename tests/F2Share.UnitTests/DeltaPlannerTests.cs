using F2Share.Application.Abstractions;
using F2Share.Application.Transfers;

namespace F2Share.UnitTests;

public sealed class DeltaPlannerTests
{
    [Fact]
    public async Task PlanAsync_UsesChunkerAndReturnsDelta()
    {
        var expected = new List<FileChunk>
        {
            new(0, 128, "A", new byte[] { 1, 2, 3 }),
            new(128, 128, "B", null)
        };

        var chunker = new FakeChunker(expected);
        var planner = new DeltaPlanner(chunker);

        var result = await planner.PlanAsync("sample.bin", ["A", "B"], 128, CancellationToken.None);

        Assert.Equal(expected.Count, result.Count);
        Assert.Equal("A", result[0].Hash);
        Assert.Null(result[1].Data);
    }

    private sealed class FakeChunker : IChunker
    {
        private readonly IReadOnlyList<FileChunk> _result;

        public FakeChunker(IReadOnlyList<FileChunk> result)
        {
            _result = result;
        }

        public Task<IReadOnlyList<FileChunk>> BuildDeltaAsync(string absolutePath, IReadOnlyList<string> remoteBlockHashes, int blockSizeBytes, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}
