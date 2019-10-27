// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Adds the minimum essential Silverback services to the specified <see cref="IServiceCollection" />. Additional services
        /// including broker support, inbound/outbound connectors and database bindings must be added separately using the 
        /// <see cref="ISilverbackBuilder"/> returned from this method.
        /// </summary>
        /// <returns></returns>
        public static ISilverbackBuilder AddSilverback(this IServiceCollection services)
        {
            services
                .AddSingleton<BusOptions>()
                .AddSingleton<BusConfigurator>()
                .AddScoped<IPublisher, Publisher>()
                .AddScoped<SubscribedMethodInvoker>()
                .AddScoped<SubscribedMethodsLoader>()
                .AddScoped<ArgumentsResolver>()
                .AddScoped<ReturnValueHandler>()
                // Note: resolvers and handlers will be evaluated in reverse order
                .AddScoped<IArgumentResolver, ServiceProviderAdditionalArgumentResolver>()
                .AddSingleton<IArgumentResolver, SingleMessageArgumentResolver>()
                .AddSingleton<IArgumentResolver, EnumerableMessageArgumentResolver>()
                .AddScoped<IReturnValueHandler, SingleMessageReturnValueHandler>()
                .AddScoped<IReturnValueHandler, EnumerableMessagesReturnValueHandler>();

            return new SilverbackBuilder(services);
        }
    }
}