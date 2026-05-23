using F2Share.Application.Abstractions;
using F2Share.Transport.Protocol;

namespace F2Share.UnitTests;

public sealed class EnvelopeFrameCodecTests
{
    [Fact]
    public async Task FramedRoundTrip_PreservesEnvelope()
    {
        var original = new TransportEnvelope(
            "device-A",
            "device-B",
            "sync.manifest",
            new byte[] { 1, 2, 3, 4, 5 },
            DateTimeOffset.UtcNow);

        await using var stream = new MemoryStream();
        await EnvelopeFrameCodec.WriteFramedAsync(stream, original, CancellationToken.None);
        stream.Position = 0;

        var decoded = await EnvelopeFrameCodec.ReadFramedAsync(stream, CancellationToken.None);

        Assert.Equal(original.FromDeviceId, decoded.FromDeviceId);
        Assert.Equal(original.ToDeviceId, decoded.ToDeviceId);
        Assert.Equal(original.MessageType, decoded.MessageType);
        Assert.Equal(original.Payload, decoded.Payload);
    }

    [Fact]
    public void Decode_ThrowsOnCorruptedData()
    {
        var corrupted = new byte[] { 255, 0, 0, 0, 0, 0 };
        Assert.Throws<InvalidOperationException>(() => EnvelopeFrameCodec.Decode(corrupted));
    }
}
