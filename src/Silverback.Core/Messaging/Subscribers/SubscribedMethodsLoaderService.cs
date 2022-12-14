// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Resolves all the subscribers and build the types cache to boost the first publish performance.
/// </summary>
public class SubscribedMethodsLoaderService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethodsLoaderService" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" />.
    /// </param>
    public SubscribedMethodsLoaderService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IServiceScope scope = _serviceScopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<SubscribedMethodsCacheSingleton>()
            .Preload(scope.ServiceProvider);

        return Task.CompletedTask;
    }
}
