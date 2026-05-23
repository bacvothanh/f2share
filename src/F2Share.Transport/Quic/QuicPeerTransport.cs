using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Authentication;
using System.Runtime.Versioning;
using System.Threading.Channels;
using F2Share.Application.Abstractions;
using F2Share.Transport.Protocol;
using Microsoft.Extensions.Logging;

namespace F2Share.Transport.Quic;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class QuicPeerTransport : IPeerTransport
{
    private readonly string _localDeviceId;
    private readonly ILogger<QuicPeerTransport> _logger;
    private readonly Channel<TransportEnvelope> _incoming = Channel.CreateUnbounded<TransportEnvelope>();
    private readonly ConcurrentDictionary<string, QuicConnection> _connections = new(StringComparer.Ordinal);
    private CancellationTokenSource? _listenCts;
    private Task? _listenTask;

    public QuicPeerTransport(string localDeviceId, ILogger<QuicPeerTransport> logger)
    {
        _localDeviceId = localDeviceId;
        _logger = logger;
    }

    public Task StartAsync(int listenPort, CancellationToken cancellationToken)
    {
        if (!QuicListener.IsSupported)
        {
            _logger.LogWarning("QUIC not supported on this platform/runtime. Transport must fallback to TCP in production.");
            return Task.CompletedTask;
        }

        _listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenTask = Task.Run(() => RunListenerAsync(listenPort, _listenCts.Token), _listenCts.Token);
        return Task.CompletedTask;
    }

    public async Task ConnectAsync(DiscoveredPeer peer, CancellationToken cancellationToken)
    {
        if (!QuicListener.IsSupported || !peer.SupportsQuic)
        {
            _logger.LogInformation("Peer {PeerId} does not support QUIC or local platform has no QUIC support", peer.DeviceId);
            return;
        }

        var options = new QuicClientConnectionOptions
        {
            RemoteEndPoint = new DnsEndPoint(peer.Host, peer.Port),
            ClientAuthenticationOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = [new SslApplicationProtocol("f2share-v1")],
                EnabledSslProtocols = SslProtocols.Tls13,
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };

        var connection = await QuicConnection.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
        _connections[peer.DeviceId] = connection;
        _logger.LogInformation("Connected to peer {PeerId} via QUIC", peer.DeviceId);
    }

    public async Task SendAsync(string peerDeviceId, TransportEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(peerDeviceId, out var connection))
        {
            throw new InvalidOperationException($"No active transport connection for peer '{peerDeviceId}'.");
        }

        await using var stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken).ConfigureAwait(false);
        await EnvelopeFrameCodec.WriteFramedAsync(stream, envelope, cancellationToken).ConfigureAwait(false);
        stream.CompleteWrites();
    }

    public async IAsyncEnumerable<TransportEnvelope> ReadIncomingAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _incoming.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_incoming.Reader.TryRead(out var env))
            {
                yield return env;
            }
        }
    }

    private async Task RunListenerAsync(int listenPort, CancellationToken cancellationToken)
    {
        var listenOptions = new QuicListenerOptions
        {
            ListenEndPoint = new IPEndPoint(IPAddress.Any, listenPort),
            ApplicationProtocols = [new SslApplicationProtocol("f2share-v1")],
            ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(new QuicServerConnectionOptions
            {
                DefaultStreamErrorCode = 0,
                DefaultCloseErrorCode = 0,
                ServerAuthenticationOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = [new SslApplicationProtocol("f2share-v1")],
                    EnabledSslProtocols = SslProtocols.Tls13,
                    ServerCertificate = CertificateLoader.CreateEphemeralSelfSignedCertificate("CN=f2share")
                }
            })
        };

        await using var listener = await QuicListener.ListenAsync(listenOptions, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            var connection = await listener.AcceptConnectionAsync(cancellationToken).ConfigureAwait(false);
            _ = Task.Run(() => HandleConnectionAsync(connection, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleConnectionAsync(QuicConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await using var stream = await connection.AcceptInboundStreamAsync(cancellationToken).ConfigureAwait(false);
                var envelope = await EnvelopeFrameCodec.ReadFramedAsync(stream, cancellationToken).ConfigureAwait(false);
                _incoming.Writer.TryWrite(envelope with { ToDeviceId = _localDeviceId });
            }
        }
        catch (EndOfStreamException)
        {
            _logger.LogDebug("Peer stream closed cleanly");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QUIC connection closed or failed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listenCts?.Cancel();

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask.ConfigureAwait(false);
            }
            catch
            {
            }
        }

        foreach (var (_, connection) in _connections)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }

        _incoming.Writer.TryComplete();
        _listenCts?.Dispose();
    }
}

internal static class CertificateLoader
{
    public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateEphemeralSelfSignedCertificate(string subjectName)
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            subjectName,
            rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return new System.Security.Cryptography.X509Certificates.X509Certificate2(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx));
    }
}
