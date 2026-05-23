using System.Text.RegularExpressions;
using F2Share.Application.Abstractions;

namespace F2Share.Infrastructure.Watcher;

public sealed class GlobPathFilter : IPathFilter
{
    private readonly IReadOnlyList<string> _includeRoots;
    private readonly IReadOnlyList<Regex> _ignoreRegexes;
    private readonly bool _syncHidden;

    public GlobPathFilter(IReadOnlyList<string>? includeRoots, IReadOnlyList<string>? ignorePatterns, bool syncHidden)
    {
        _includeRoots = (includeRoots ?? []).Select(Normalize).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        _ignoreRegexes = (ignorePatterns ?? []).Select(BuildRegex).ToArray();
        _syncHidden = syncHidden;
    }

    public bool ShouldSync(string relativePath, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
        {
            return false;
        }

        var normalized = Normalize(relativePath);

        if (!_syncHidden && IsHiddenSegment(normalized))
        {
            return false;
        }

        if (_includeRoots.Count > 0 && !_includeRoots.Any(root => normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (_ignoreRegexes.Any(regex => regex.IsMatch(normalized)))
        {
            return false;
        }

        if (!isDirectory && normalized.StartsWith(".f2share/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsHiddenSegment(string normalizedPath)
    {
        var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => part.Length > 1 && part[0] == '.');
    }

    private static Regex BuildRegex(string pattern)
    {
        var normalized = Normalize(pattern);
        var regexPattern = Regex.Escape(normalized)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".");

        return new Regex($"^{regexPattern}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/').Trim().TrimStart('/');
    }
}
