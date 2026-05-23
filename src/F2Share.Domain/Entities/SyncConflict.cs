using F2Share.Domain.ValueObjects;

namespace F2Share.Domain.Entities;

public sealed class SyncConflict
{
    public required string RelativePath { get; init; }
    public required string LocalDeviceId { get; init; }
    public required string RemoteDeviceId { get; init; }
    public required VersionVector LocalVersion { get; init; }
    public required VersionVector RemoteVersion { get; init; }
    public required DateTimeOffset DetectedAtUtc { get; init; }

    public string BuildConflictFileName(string hostDeviceName)
    {
        var extension = Path.GetExtension(RelativePath);
        var baseName = Path.GetFileNameWithoutExtension(RelativePath);
        var dir = Path.GetDirectoryName(RelativePath);
        var stamp = DetectedAtUtc.ToString("yyyyMMddHHmmss");
        var file = $"{baseName}.conflict-{hostDeviceName}-{stamp}{extension}";
        return string.IsNullOrWhiteSpace(dir) ? file : Path.Combine(dir, file);
    }
}
