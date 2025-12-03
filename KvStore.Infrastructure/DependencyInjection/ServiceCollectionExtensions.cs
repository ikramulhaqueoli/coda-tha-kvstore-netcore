using KvStore.Core.Application.Abstractions;
using KvStore.Core.Domain.Repositories;
using KvStore.Infrastructure.Concurrency;
using KvStore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace KvStore.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKvStoreInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IKeyLockProvider, PerKeySemaphoreLockProvider>();
        services.AddSingleton<IKeyValueRepository, InMemoryKeyValueRepository>();
        return services;
    }
}

