using F2Share.Application.Abstractions;
using F2Share.Application.Background;
using F2Share.Application.Sync;
using F2Share.Application.Transfers;
using F2Share.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace F2Share.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddF2ShareApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IConflictResolutionService, ConflictResolutionService>();
        services.AddSingleton<DeltaPlanner>();
        services.AddSingleton<SyncOrchestrator>();
        services.AddSingleton<ITransferScheduler, ChannelTransferScheduler>();
        services.AddHostedService<SyncWorker>();

        return services;
    }
}
