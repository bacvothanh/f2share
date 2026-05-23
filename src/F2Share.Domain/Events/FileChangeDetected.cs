namespace F2Share.Domain.Events;

public sealed record FileChangeDetected(
    string ShareId,
    string RootPath,
    string RelativePath,
    FileChangeKind ChangeKind,
    DateTimeOffset ObservedAtUtc,
    long? Length = null,
    string? RenamedFromRelativePath = null);

public enum FileChangeKind
{
    Created = 0,
    Modified = 1,
    Deleted = 2,
    Renamed = 3,
    DirectoryCreated = 4,
    DirectoryDeleted = 5,
    DirectoryRenamed = 6
}
