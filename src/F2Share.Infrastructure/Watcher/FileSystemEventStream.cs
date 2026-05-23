using System.Threading.Channels;
using F2Share.Application.Abstractions;
using F2Share.Domain.Events;

namespace F2Share.Infrastructure.Watcher;

public sealed class FileSystemEventStream : IFileSystemEventStream
{
    private readonly string _shareId;
    private readonly string _rootPath;
    private readonly IPathFilter _pathFilter;
    private readonly Channel<FileChangeDetected> _channel = Channel.CreateUnbounded<FileChangeDetected>();
    private readonly List<FileSystemWatcher> _watchers = [];

    public FileSystemEventStream(string shareId, string rootPath, IPathFilter pathFilter)
    {
        _shareId = shareId;
        _rootPath = rootPath;
        _pathFilter = pathFilter;
    }

    public ChannelReader<FileChangeDetected> Reader => _channel.Reader;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var watcher = new FileSystemWatcher(_rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Created += (_, e) => Publish(e.FullPath, FileChangeKind.Created, null);
        watcher.Changed += (_, e) => Publish(e.FullPath, FileChangeKind.Modified, null);
        watcher.Deleted += (_, e) => Publish(e.FullPath, FileChangeKind.Deleted, null);
        watcher.Renamed += (_, e) => Publish(e.FullPath, FileChangeKind.Renamed, e.OldFullPath);
        watcher.EnableRaisingEvents = true;

        _watchers.Add(watcher);
        return Task.CompletedTask;
    }

    private void Publish(string fullPath, FileChangeKind kind, string? oldFullPath)
    {
        var relativePath = Normalize(Path.GetRelativePath(_rootPath, fullPath));
        var oldRelativePath = oldFullPath is null ? null : Normalize(Path.GetRelativePath(_rootPath, oldFullPath));

        var currentIsDirectory = Directory.Exists(fullPath);
        var currentIncluded = _pathFilter.ShouldSync(relativePath, currentIsDirectory);

        if (kind == FileChangeKind.Renamed && oldRelativePath is not null)
        {
            var oldIncluded = _pathFilter.ShouldSync(oldRelativePath, currentIsDirectory);

            if (oldIncluded && !currentIncluded)
            {
                Emit(oldRelativePath, oldRelativePath, FileChangeKind.Deleted, null);
                return;
            }

            if (!oldIncluded && currentIncluded)
            {
                Emit(relativePath, null, currentIsDirectory ? FileChangeKind.DirectoryCreated : FileChangeKind.Created, null);
                return;
            }

            if (!oldIncluded && !currentIncluded)
            {
                return;
            }

            Emit(relativePath, oldRelativePath, currentIsDirectory ? FileChangeKind.DirectoryRenamed : FileChangeKind.Renamed, null);
            return;
        }

        if (!currentIncluded)
        {
            return;
        }

        if (kind == FileChangeKind.Created && currentIsDirectory)
        {
            kind = FileChangeKind.DirectoryCreated;
        }

        Emit(relativePath, oldRelativePath, kind, File.Exists(fullPath) ? new FileInfo(fullPath).Length : null);
    }

    private void Emit(string relativePath, string? oldRelativePath, FileChangeKind kind, long? length)
    {
        var normalizedPath = Normalize(relativePath);
        var normalizedOld = oldRelativePath is null ? null : Normalize(oldRelativePath);

        var evt = new FileChangeDetected(
            _shareId,
            _rootPath,
            normalizedPath,
            kind,
            DateTimeOffset.UtcNow,
            length,
            normalizedOld);

        _channel.Writer.TryWrite(evt);
    }

    private static string Normalize(string relativePath) => relativePath.Replace('\\', '/');

    public ValueTask DisposeAsync()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
