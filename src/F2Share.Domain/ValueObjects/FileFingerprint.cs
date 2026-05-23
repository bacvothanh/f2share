namespace F2Share.Domain.ValueObjects;

public readonly record struct FileFingerprint(
    string RelativePath,
    long Length,
    DateTimeOffset LastWriteUtc,
    string StrongHash,
    IReadOnlyList<string> BlockHashes);
