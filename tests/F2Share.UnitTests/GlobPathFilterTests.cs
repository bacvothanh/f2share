using F2Share.Infrastructure.Watcher;

namespace F2Share.UnitTests;

public sealed class GlobPathFilterTests
{
    [Fact]
    public void ShouldSync_RespectsIgnorePattern()
    {
        var filter = new GlobPathFilter([], ["**/*.tmp", "**/node_modules/**"], syncHidden: false);

        Assert.False(filter.ShouldSync("docs/cache.tmp", isDirectory: false));
        Assert.False(filter.ShouldSync("web/node_modules/react/index.js", isDirectory: false));
        Assert.True(filter.ShouldSync("docs/readme.md", isDirectory: false));
    }

    [Fact]
    public void ShouldSync_RespectsIncludeRoots()
    {
        var filter = new GlobPathFilter(["teamA/", "shared/"], [], syncHidden: false);

        Assert.True(filter.ShouldSync("teamA/report.txt", isDirectory: false));
        Assert.True(filter.ShouldSync("shared/data.csv", isDirectory: false));
        Assert.False(filter.ShouldSync("teamB/notes.txt", isDirectory: false));
    }
}
