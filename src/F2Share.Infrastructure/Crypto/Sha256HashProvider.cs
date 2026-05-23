using System.Security.Cryptography;
using F2Share.Application.Abstractions;

namespace F2Share.Infrastructure.Crypto;

public sealed class Sha256HashProvider : IHashProvider, IChunker
{
    public async Task<string> ComputeStrongHashAsync(string absolutePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var hasher = SHA256.Create();
        var hash = await hasher.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    public async Task<IReadOnlyList<string>> ComputeBlockHashesAsync(string absolutePath, int blockSizeBytes, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var hasher = SHA256.Create();

        var hashes = new List<string>();
        var buffer = new byte[blockSizeBytes];

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            var hash = hasher.ComputeHash(buffer, 0, read);
            hashes.Add(Convert.ToHexString(hash));
        }

        return hashes;
    }

    public async Task<IReadOnlyList<FileChunk>> BuildDeltaAsync(
        string absolutePath,
        IReadOnlyList<string> remoteBlockHashes,
        int blockSizeBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var hasher = SHA256.Create();

        var chunks = new List<FileChunk>();
        var buffer = new byte[blockSizeBytes];
        var offset = 0L;
        var index = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            var blockHashBytes = hasher.ComputeHash(buffer, 0, read);
            var blockHash = Convert.ToHexString(blockHashBytes);
            var existsRemote = index < remoteBlockHashes.Count && string.Equals(remoteBlockHashes[index], blockHash, StringComparison.Ordinal);

            chunks.Add(new FileChunk(offset, read, blockHash, existsRemote ? null : buffer[..read].ToArray()));

            offset += read;
            index++;
        }

        return chunks;
    }
}
