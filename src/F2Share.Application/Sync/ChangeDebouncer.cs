using System.Collections.Concurrent;
using F2Share.Domain.Events;

namespace F2Share.Application.Sync;

public sealed class ChangeDebouncer
{
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, FileChangeDetected> _pending = new(StringComparer.OrdinalIgnoreCase);

    public ChangeDebouncer(TimeSpan window)
    {
        _window = window;
    }

    public bool TryAdd(FileChangeDetected evt)
    {
        var key = $"{evt.ShareId}:{evt.RelativePath}";
        var existing = _pending.GetOrAdd(key, evt);

        if (ReferenceEquals(existing, evt))
        {
            return true;
        }

        if (evt.ObservedAtUtc - existing.ObservedAtUtc <= _window)
        {
            _pending[key] = evt;
            return false;
        }

        _pending[key] = evt;
        return true;
    }
}
