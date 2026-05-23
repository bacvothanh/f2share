using MessagePack;

namespace F2Share.Transport.Protocol;

[MessagePackObject]
public sealed class HelloMessage
{
    [Key(0)] public required string DeviceId { get; init; }
    [Key(1)] public required string DisplayName { get; init; }
    [Key(2)] public required string PublicKeyFingerprint { get; init; }
    [Key(3)] public required long TimestampUnixMs { get; init; }
    [Key(4)] public required byte[] Signature { get; init; }
}

[MessagePackObject]
public sealed class SyncManifestMessage
{
    [Key(0)] public required string ShareId { get; init; }
    [Key(1)] public required string RelativePath { get; init; }
    [Key(2)] public required string StrongHash { get; init; }
    [Key(3)] public required long Length { get; init; }
    [Key(4)] public required List<string> BlockHashes { get; init; }
    [Key(5)] public required bool IsDeleted { get; init; }
    [Key(6)] public required long ModifiedUnixMs { get; init; }
}

[MessagePackObject]
public sealed class ChunkTransferMessage
{
    [Key(0)] public required string ShareId { get; init; }
    [Key(1)] public required string RelativePath { get; init; }
    [Key(2)] public required long Offset { get; init; }
    [Key(3)] public required int Length { get; init; }
    [Key(4)] public required string Hash { get; init; }
    [Key(5)] public required byte[] Data { get; init; }
    [Key(6)] public required bool IsFinalChunk { get; init; }
}

[MessagePackObject]
public sealed class AckMessage
{
    [Key(0)] public required string CorrelationId { get; init; }
    [Key(1)] public required bool Accepted { get; init; }
    [Key(2)] public required string? ErrorCode { get; init; }
}
