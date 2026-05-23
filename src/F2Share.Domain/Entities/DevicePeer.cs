namespace F2Share.Domain.Entities;

public sealed class DevicePeer
{
    public required string DeviceId { get; init; }
    public required string DisplayName { get; set; }
    public required string PublicKeyFingerprint { get; init; }
    public required IReadOnlyList<PeerEndpoint> Endpoints { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }
    public bool IsTrusted { get; set; }
}

public readonly record struct PeerEndpoint(string Host, int Port, bool SupportsQuic, bool SupportsTcpFallback);
