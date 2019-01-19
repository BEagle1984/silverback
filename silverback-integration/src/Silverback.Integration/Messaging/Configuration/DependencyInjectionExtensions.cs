// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBroker<T>(this IServiceCollection services, Action<BrokerOptionsBuilder> optionsAction = null)
            where T : class, IBroker
        {
            services
                .AddSingleton<IBroker, T>()
                .AddSingleton<ErrorPolicyBuilder>();

            var options = new BrokerOptionsBuilder(services);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return services;
        }

        // TODO: Support & Test
        //public static IServiceCollection AddSecondaryBroker<T>(this IServiceCollection services, Action<BrokerOptionsBuilder> optionsAction = null)
        //    where T : class, IBroker
        //{
        //    services
        //        .AddSingleton<IBroker, T>();

        //    var options = new BrokerOptionsBuilder(services);
        //    optionsAction?.Invoke(options);
        //    options.CompleteWithDefaults();

        //    return services;
        //}
    }
}
