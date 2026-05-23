using F2Share.Domain.Entities;
using F2Share.Domain.ValueObjects;

namespace F2Share.Domain.Services;

public interface IConflictResolutionService
{
    ConflictResolution Resolve(
        string relativePath,
        string localDeviceId,
        string remoteDeviceId,
        VersionVector local,
        VersionVector remote,
        DateTimeOffset nowUtc,
        string localHostName);
}

public sealed class ConflictResolutionService : IConflictResolutionService
{
    public ConflictResolution Resolve(
        string relativePath,
        string localDeviceId,
        string remoteDeviceId,
        VersionVector local,
        VersionVector remote,
        DateTimeOffset nowUtc,
        string localHostName)
    {
        var comparison = VersionVector.Compare(local, remote);

        return comparison switch
        {
            VersionComparison.LeftDominates => ConflictResolution.KeepLocal(),
            VersionComparison.RightDominates => ConflictResolution.AcceptRemote(),
            VersionComparison.Equal => ConflictResolution.NoOp(),
            _ => BuildConcurrentResolution(relativePath, localDeviceId, remoteDeviceId, local, remote, nowUtc, localHostName)
        };
    }

    private static ConflictResolution BuildConcurrentResolution(
        string relativePath,
        string localDeviceId,
        string remoteDeviceId,
        VersionVector local,
        VersionVector remote,
        DateTimeOffset nowUtc,
        string localHostName)
    {
        var conflict = new SyncConflict
        {
            RelativePath = relativePath,
            LocalDeviceId = localDeviceId,
            RemoteDeviceId = remoteDeviceId,
            LocalVersion = local,
            RemoteVersion = remote,
            DetectedAtUtc = nowUtc
        };

        return ConflictResolution.CreateConflict(conflict.BuildConflictFileName(localHostName));
    }
}

public readonly record struct ConflictResolution(ConflictAction Action, string? ConflictFileName)
{
    public static ConflictResolution KeepLocal() => new(ConflictAction.KeepLocal, null);
    public static ConflictResolution AcceptRemote() => new(ConflictAction.AcceptRemote, null);
    public static ConflictResolution NoOp() => new(ConflictAction.NoOp, null);
    public static ConflictResolution CreateConflict(string conflictFileName) => new(ConflictAction.CreateConflictCopy, conflictFileName);
}

public enum ConflictAction
{
    NoOp = 0,
    KeepLocal = 1,
    AcceptRemote = 2,
    CreateConflictCopy = 3
}
