// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddSilverback</c> method to the <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionAddSilverbackExtensions
    {
        /// <summary>
        ///     Adds the minimum essential Silverback services to the <see cref="IServiceCollection" />. Additional
        ///     services including broker support, inbound/outbound connectors and database bindings must be added
        ///     separately using the returned <see cref="ISilverbackBuilder" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="IServiceCollection" /> to add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> to add the services necessary to enable the Silverback
        ///     features.
        /// </returns>
        public static ISilverbackBuilder AddSilverback(this IServiceCollection services)
        {
            if (!services.ContainsAny<IBusOptions>())
            {
                services
                    .AddSingleton<IBusOptions>(new BusOptions())
                    .AddScoped<IPublisher, Publisher>()
                    .AddScoped<IStreamPublisher, StreamPublisher>()
                    .AddScoped<IBehaviorsProvider, BehaviorsProvider>()
                    .AddSingleton<SubscribedMethodsCacheSingleton>()
                    .AddScoped<SubscribedMethodsCache>()
                    .AddScoped<ISubscribedMethodsCache>(
                        serviceProvider => serviceProvider.GetRequiredService<SubscribedMethodsCache>())
                    .AddLogger()
                    .AddArgumentResolvers()
                    .AddReturnValueHandlers()
                    .AddSingleton<IHostedService, SubscribedMethodsLoaderService>();
            }

            return new SilverbackBuilder(services);
        }

        private static IServiceCollection AddLogger(this IServiceCollection services) => services
            .AddSingleton(typeof(ISilverbackLogger<>), typeof(SilverbackLogger<>))
            .AddSingleton(typeof(IMappedLevelsLogger<>), typeof(MappedLevelsLogger<>))
            .AddSingleton<ILogLevelDictionary, LogLevelDictionary>();

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
            .AddScoped<IReturnValueHandler, EnumerableMessagesReturnValueHandler>()
            .AddScoped<IReturnValueHandler, ReadOnlyCollectionMessagesReturnValueHandler>()
            .AddScoped<IReturnValueHandler, AsyncEnumerableMessagesReturnValueHandler>();
    }
}
