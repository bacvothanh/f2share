using F2Share.Application.Abstractions;
using F2Share.Application.Sync;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace F2Share.Application.Background;

public sealed class SyncWorker : BackgroundService
{
    private readonly SyncOrchestrator _orchestrator;
    private readonly IPeerTransport _peerTransport;
    private readonly ILogger<SyncWorker> _logger;

    public SyncWorker(SyncOrchestrator orchestrator, IPeerTransport peerTransport, ILogger<SyncWorker> logger)
    {
        _orchestrator = orchestrator;
        _peerTransport = peerTransport;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync worker started");

        await _peerTransport.StartAsync(40177, stoppingToken).ConfigureAwait(false);
        await _orchestrator.RunAsync(stoppingToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sync worker stopping");
        await _peerTransport.DisposeAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
