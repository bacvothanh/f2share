namespace F2Share.Domain.ValueObjects;

public sealed class VersionVector : IEquatable<VersionVector>
{
    private readonly Dictionary<string, long> _versions = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, long> Values => _versions;

    public long this[string deviceId] => _versions.TryGetValue(deviceId, out var value) ? value : 0;

    public VersionVector Increment(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        _versions[deviceId] = this[deviceId] + 1;
        return this;
    }

    public VersionVector Merge(VersionVector other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var (deviceId, version) in other._versions)
        {
            if (!_versions.TryGetValue(deviceId, out var existing) || version > existing)
            {
                _versions[deviceId] = version;
            }
        }

        return this;
    }

    public static VersionComparison Compare(VersionVector left, VersionVector right)
    {
        var allKeys = left._versions.Keys.Union(right._versions.Keys, StringComparer.Ordinal);
        var leftGreater = false;
        var rightGreater = false;

        foreach (var key in allKeys)
        {
            var l = left[key];
            var r = right[key];
            if (l > r) leftGreater = true;
            if (r > l) rightGreater = true;
        }

        return (leftGreater, rightGreater) switch
        {
            (true, false) => VersionComparison.LeftDominates,
            (false, true) => VersionComparison.RightDominates,
            (false, false) => VersionComparison.Equal,
            _ => VersionComparison.Concurrent
        };
    }

    public bool Equals(VersionVector? other)
    {
        if (other is null) return false;
        return Compare(this, other) == VersionComparison.Equal;
    }

    public override bool Equals(object? obj) => obj is VersionVector other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in _versions.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key, StringComparer.Ordinal);
            hash.Add(kvp.Value);
        }

        return hash.ToHashCode();
    }
}

public enum VersionComparison
{
    Equal = 0,
    LeftDominates = 1,
    RightDominates = 2,
    Concurrent = 3
}
