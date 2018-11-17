using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBroker<T>(this IServiceCollection services, Action<BrokerOptionsBuilder> optionsAction)
            where T : class, IBroker
        {
            services
                .AddSingleton<IBroker, T>()
                .AddSingleton<IBrokerEndpointsConfigurationBuilder, BrokerEndpointsConfigurationBuilder>()
                .AddSingleton<ErrorPolicyBuilder>();

            var options = new BrokerOptionsBuilder(services);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return services;
        }
    }
}
