// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddSilverback </c> method to the <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionAddSilverbackExtensions
    {
        /// <summary>
        ///     Adds the minimum essential Silverback services to the <see cref="IServiceCollection" />.
        ///     Additional services including broker support, inbound/outbound connectors and database bindings
        ///     must be added separately using the returned <see cref="ISilverbackBuilder" />.
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
            services
                .AddSingleton<BusOptions>()
                .AddSingleton<IBusConfigurator, BusConfigurator>()
                .AddScoped<IPublisher, Publisher>()
                .AddScoped<SubscribedMethodInvoker>()
                .AddScoped<SubscribedMethodsLoader>()
                .AddScoped<ArgumentsResolverService>()
                .AddScoped<ReturnValueHandlerService>()

                // Note: resolvers and handlers will be evaluated in reverse order
                .AddScoped<IArgumentResolver, ServiceProviderAdditionalArgumentResolver>()
                .AddSingleton<IArgumentResolver, SingleMessageArgumentResolver>()
                .AddSingleton<IArgumentResolver, EnumerableMessageArgumentResolver>()
                .AddSingleton<IArgumentResolver, ReadOnlyCollectionMessageArgumentResolver>()
                .AddScoped<IReturnValueHandler, SingleMessageReturnValueHandler>()
                .AddScoped<IReturnValueHandler, EnumerableMessagesReturnValueHandler>()
                .AddScoped<IReturnValueHandler, ReadOnlyCollectionMessagesReturnValueHandler>();

            return new SilverbackBuilder(services);
        }
    }
}
