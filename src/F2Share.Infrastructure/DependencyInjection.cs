using F2Share.Application.Abstractions;
using F2Share.Infrastructure.Crypto;
using F2Share.Infrastructure.Discovery;
using F2Share.Infrastructure.Persistence;
using F2Share.Infrastructure.Watcher;
using Microsoft.Extensions.DependencyInjection;

namespace F2Share.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddF2ShareInfrastructure(
        this IServiceCollection services,
        string shareId,
        string rootPath,
        string dbPath,
        string deviceId,
        string displayName,
        int port,
        ShareSyncOptions? shareSyncOptions = null)
    {
        var options = shareSyncOptions ?? new ShareSyncOptions([], ["**/.git/**", "**/node_modules/**", "**/*.tmp", "**/*.swp"], SyncHidden: false);

        services.AddSingleton<IPathFilter>(_ => new GlobPathFilter(options.IncludeRoots, options.IgnorePatterns, options.SyncHidden));
        services.AddSingleton<IFileSystemEventStream>(provider =>
            new FileSystemEventStream(shareId, rootPath, provider.GetRequiredService<IPathFilter>()));
        services.AddSingleton<IMetadataStore>(_ => new SqliteMetadataStore(dbPath));

        services.AddSingleton<Sha256HashProvider>();
        services.AddSingleton<IHashProvider>(provider => provider.GetRequiredService<Sha256HashProvider>());
        services.AddSingleton<IChunker>(provider => provider.GetRequiredService<Sha256HashProvider>());

        services.AddSingleton<IDiscoveryService>(_ => new LanDiscoveryService(deviceId, displayName, port));

        return services;
    }
}
