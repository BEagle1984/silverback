// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

// ReSharper disable once CheckNamespace
namespace Silverback.Messaging.Configuration
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBus(this IServiceCollection services, Action<BusPluginOptions> optionsAction = null)
        {

            var pluginOptions = new BusPluginOptions(services);
            optionsAction?.Invoke(pluginOptions);

            return services
                .AddSingleton<BusOptions>()
                .AddSingleton<BusConfigurator>()
                .AddScoped<SubscribedMethodInvoker>()
                .AddScoped<SubscribedMethodArgumentsResolver>()
                .AddScoped<IArgumentResolver, EnumerableMessageArgumentResolver>()
                .AddScoped<IArgumentResolver, SingleMessageArgumentResolver>()
                .AddScoped<IArgumentResolver, ServiceProviderAdditionalArgumentResolver>()
                .AddScoped<ReturnValueHandler>()
                .AddScoped<IReturnValueHandler, EnumerableMessagesReturnValueHandler>()
                .AddScoped<IReturnValueHandler, SingleMessageReturnValueHandler>()
                .AddScoped<IPublisher, Publisher>();
        }
    }
}