using F2Share.Application.Sync;
using F2Share.Domain.Events;

namespace F2Share.IntegrationTests;

public sealed class ChangeDebouncerIntegrationTests
{
    [Fact]
    public void Debouncer_CollapsesRapidEventsForSamePath()
    {
        var debouncer = new ChangeDebouncer(TimeSpan.FromMilliseconds(500));
        var first = new FileChangeDetected("s1", "C:\\data", "a.txt", FileChangeKind.Modified, DateTimeOffset.UtcNow);
        var second = first with { ObservedAtUtc = first.ObservedAtUtc.AddMilliseconds(120) };

        var acceptedFirst = debouncer.TryAdd(first);
        var acceptedSecond = debouncer.TryAdd(second);

        Assert.True(acceptedFirst);
        Assert.False(acceptedSecond);
    }
}
