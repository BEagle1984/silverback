// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="AddSilverback" /> and <see cref="ConfigureSilverback" /> methods to the <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the minimum essential Silverback services to the <see cref="IServiceCollection" />. Additional services including
    ///     message broker support and database bindings must be added separately using the returned <see cref="SilverbackBuilder" />.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> to add the services necessary to enable the Silverback features.
    /// </returns>
    public static SilverbackBuilder AddSilverback(this IServiceCollection services)
    {
        if (!services.ContainsAny<BusOptions>())
        {
            services
                .AddSingleton(new BusOptions())
                .AddScoped<IPublisher, Publisher>()
                .AddScoped<IStreamPublisher, StreamPublisher>()
                .AddScoped<IBehaviorsProvider, BehaviorsProvider>()
                .AddSingleton<SubscribedMethodsCacheSingleton>()
                .AddScoped<SubscribedMethodsCache>()
                .AddLogger()
                .AddArgumentResolvers()
                .AddReturnValueHandlers()
                .AddSingleton<IHostedService, SubscribedMethodsLoaderService>()
                .AddSingleton<IDistributedLockFactory>(serviceProvider => serviceProvider.GetService<DistributedLockFactory>())
                .AddSingleton(new DistributedLockFactory());
        }

        return new SilverbackBuilder(services);
    }

    /// <summary>
    ///     Returns an <see cref="SilverbackBuilder" /> instance that can be used to configure the additional services.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> to add the services necessary to enable the Silverback features.
    /// </returns>
    public static SilverbackBuilder ConfigureSilverback(this IServiceCollection services) => AddSilverback(services);

    private static IServiceCollection AddLogger(this IServiceCollection services) => services
        .AddSingleton(typeof(ISilverbackLogger<>), typeof(SilverbackLogger<>))
        .AddSingleton(typeof(IMappedLevelsLogger<>), typeof(MappedLevelsLogger<>))
        .AddSingleton<LogLevelDictionary>();

    // Note: resolvers and handlers will be evaluated in reverse order
    private static IServiceCollection AddArgumentResolvers(this IServiceCollection services) => services
        .AddSingleton<ArgumentsResolversRepository>()
        .AddSingleton<IArgumentResolver, DefaultAdditionalArgumentResolver>()
        .AddSingleton<IArgumentResolver, SingleMessageArgumentResolver>()
        .AddSingleton<IArgumentResolver, StreamEnumerableMessageArgumentResolver>();

    // Note: resolvers and handlers will be evaluated in reverse order
    private static IServiceCollection AddReturnValueHandlers(this IServiceCollection services) => services
        .AddScoped<ReturnValueHandlerService>()
        .AddScoped<IReturnValueHandler, SingleMessageReturnValueHandler>()
        .AddScoped<IReturnValueHandler, EnumerableMessagesReturnValueHandler>();
}
