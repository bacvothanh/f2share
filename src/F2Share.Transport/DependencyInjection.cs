using F2Share.Application.Abstractions;
using F2Share.Transport.Quic;
using Microsoft.Extensions.DependencyInjection;

namespace F2Share.Transport;

public static class DependencyInjection
{
    public static IServiceCollection AddF2ShareTransport(this IServiceCollection services, string localDeviceId)
    {
        services.AddSingleton<IPeerTransport>(provider =>
            new QuicPeerTransport(
                localDeviceId,
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<QuicPeerTransport>>()));

        return services;
    }
}
