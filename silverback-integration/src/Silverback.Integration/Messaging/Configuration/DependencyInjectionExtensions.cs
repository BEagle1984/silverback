using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBroker<T>(this IServiceCollection services, Action<BrokerOptions> optionsBuilder)
            where T : class, IBroker
        {
            services.AddSingleton<IBroker, T>();

            var options = new BrokerOptions(services);
            optionsBuilder?.Invoke(options);
            options.CompleteWithDefaults();

            return services;
        }

        //public static IApplicationBuilder ConnectBroker(this IApplicationBuilder app)
        //{
        //    app.resolve<InboundConnector>();
        //    .. -> call func to bind endpoints
        //    app.resolve<Broker>().connect();
        //}
    }
}
