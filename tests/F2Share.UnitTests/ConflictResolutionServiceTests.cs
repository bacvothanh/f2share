using F2Share.Domain.Services;
using F2Share.Domain.ValueObjects;

namespace F2Share.UnitTests;

public sealed class ConflictResolutionServiceTests
{
    [Fact]
    public void ConcurrentVectors_CreateConflictCopy()
    {
        var service = new ConflictResolutionService();
        var local = new VersionVector().Increment("local");
        var remote = new VersionVector().Increment("remote");

        var result = service.Resolve(
            "notes.txt",
            "local",
            "remote",
            local,
            remote,
            DateTimeOffset.Parse("2026-05-23T09:30:00Z"),
            "device-a");

        Assert.Equal(ConflictAction.CreateConflictCopy, result.Action);
        Assert.NotNull(result.ConflictFileName);
        Assert.Contains(".conflict-device-a", result.ConflictFileName!, StringComparison.Ordinal);
    }
}
