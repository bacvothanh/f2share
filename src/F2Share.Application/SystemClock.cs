using F2Share.Application.Abstractions;

namespace F2Share.Application;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
