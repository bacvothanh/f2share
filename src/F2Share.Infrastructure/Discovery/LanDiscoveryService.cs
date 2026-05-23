using System.Net;
using System.Net.Sockets;
using System.Text;
using F2Share.Application.Abstractions;

namespace F2Share.Infrastructure.Discovery;

public sealed class LanDiscoveryService : IDiscoveryService
{
    private readonly string _deviceId;
    private readonly string _displayName;
    private readonly int _port;

    public LanDiscoveryService(string deviceId, string displayName, int port)
    {
        _deviceId = deviceId;
        _displayName = displayName;
        _port = port;
    }

    public async IAsyncEnumerable<DiscoveredPeer> DiscoverAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var udp = new UdpClient(0) { EnableBroadcast = true };
        var endpoint = new IPEndPoint(IPAddress.Broadcast, 40178);
        var payload = Encoding.UTF8.GetBytes($"F2SHARE|{_deviceId}|{_displayName}|{_port}|QUIC");

        while (!cancellationToken.IsCancellationRequested)
        {
            await udp.SendAsync(payload, payload.Length, endpoint).ConfigureAwait(false);

            var receiveTask = udp.ReceiveAsync(cancellationToken).AsTask();
            var completed = await Task.WhenAny(receiveTask, Task.Delay(750, cancellationToken)).ConfigureAwait(false);
            if (completed == receiveTask)
            {
                var packet = receiveTask.Result;
                var text = Encoding.UTF8.GetString(packet.Buffer);
                var parts = text.Split('|');
                if (parts.Length == 5 && parts[0] == "F2SHARE" && !string.Equals(parts[1], _deviceId, StringComparison.Ordinal))
                {
                    yield return new DiscoveredPeer(parts[1], parts[2], packet.RemoteEndPoint.Address.ToString(), int.Parse(parts[3]), SupportsQuic: parts[4] == "QUIC");
                }
            }
        }
    }
}
