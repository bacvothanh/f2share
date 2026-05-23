using F2Share.Domain.ValueObjects;

namespace F2Share.Domain.Entities;

public sealed class SyncItem
{
    public required string RelativePath { get; init; }
    public bool IsDirectory { get; init; }
    public long Length { get; private set; }
    public bool IsDeleted { get; private set; }
    public VersionVector Version { get; } = new();
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? StrongHash { get; private set; }

    public void ApplyUpdate(string deviceId, long length, string? strongHash, DateTimeOffset updatedAtUtc)
    {
        Length = length;
        IsDeleted = false;
        StrongHash = strongHash;
        UpdatedAtUtc = updatedAtUtc;
        Version.Increment(deviceId);
    }

    public void MarkDeleted(string deviceId, DateTimeOffset updatedAtUtc)
    {
        IsDeleted = true;
        UpdatedAtUtc = updatedAtUtc;
        Version.Increment(deviceId);
    }
}
