using System.Buffers.Binary;
using System.Text;
using F2Share.Application.Abstractions;

namespace F2Share.Transport.Protocol;

public static class EnvelopeFrameCodec
{
    private const byte Version = 1;

    public static byte[] Encode(TransportEnvelope envelope)
    {
        var messageTypeBytes = Encoding.UTF8.GetBytes(envelope.MessageType);
        var fromBytes = Encoding.UTF8.GetBytes(envelope.FromDeviceId);
        var toBytes = Encoding.UTF8.GetBytes(envelope.ToDeviceId);
        var payloadBytes = envelope.Payload;

        var size = 1 + 8 + 4 + messageTypeBytes.Length + 4 + fromBytes.Length + 4 + toBytes.Length + 4 + payloadBytes.Length;
        var buffer = new byte[size];
        var offset = 0;

        buffer[offset++] = Version;
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(offset, 8), envelope.CreatedAtUtc.ToUnixTimeMilliseconds());
        offset += 8;

        offset = WriteLengthPrefixed(buffer, offset, messageTypeBytes);
        offset = WriteLengthPrefixed(buffer, offset, fromBytes);
        offset = WriteLengthPrefixed(buffer, offset, toBytes);
        _ = WriteLengthPrefixed(buffer, offset, payloadBytes);

        return buffer;
    }

    public static TransportEnvelope Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 1 + 8 + 4 * 4)
        {
            throw new InvalidOperationException("Frame payload is too short.");
        }

        var offset = 0;
        var version = buffer[offset++];
        if (version != Version)
        {
            throw new InvalidOperationException($"Unsupported transport envelope frame version '{version}'.");
        }

        var createdUnixMs = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(offset, 8));
        offset += 8;

        var messageTypeBytes = ReadLengthPrefixed(buffer, ref offset);
        var fromBytes = ReadLengthPrefixed(buffer, ref offset);
        var toBytes = ReadLengthPrefixed(buffer, ref offset);
        var payloadBytes = ReadLengthPrefixed(buffer, ref offset);

        return new TransportEnvelope(
            Encoding.UTF8.GetString(fromBytes),
            Encoding.UTF8.GetString(toBytes),
            Encoding.UTF8.GetString(messageTypeBytes),
            payloadBytes.ToArray(),
            DateTimeOffset.FromUnixTimeMilliseconds(createdUnixMs));
    }

    public static async Task WriteFramedAsync(Stream stream, TransportEnvelope envelope, CancellationToken cancellationToken)
    {
        var body = Encode(envelope);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, body.Length);

        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<TransportEnvelope> ReadFramedAsync(Stream stream, CancellationToken cancellationToken)
    {
        var lengthHeader = new byte[4];
        await ReadExactlyAsync(stream, lengthHeader, cancellationToken).ConfigureAwait(false);

        var bodyLength = BinaryPrimitives.ReadInt32LittleEndian(lengthHeader);
        if (bodyLength <= 0 || bodyLength > 64 * 1024 * 1024)
        {
            throw new InvalidOperationException("Invalid frame length.");
        }

        var body = new byte[bodyLength];
        await ReadExactlyAsync(stream, body, cancellationToken).ConfigureAwait(false);

        return Decode(body);
    }

    private static int WriteLengthPrefixed(byte[] buffer, int offset, byte[] value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), value.Length);
        offset += 4;
        value.CopyTo(buffer.AsSpan(offset, value.Length));
        return offset + value.Length;
    }

    private static ReadOnlySpan<byte> ReadLengthPrefixed(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var len = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset, 4));
        offset += 4;

        if (len < 0 || offset + len > buffer.Length)
        {
            throw new InvalidOperationException("Corrupted frame payload.");
        }

        var result = buffer.Slice(offset, len);
        offset += len;
        return result;
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected EOF while reading transport frame.");
            }

            offset += read;
        }
    }
}
