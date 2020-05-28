// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     This class will be located via assembly scanning and invoked when a <see cref="RabbitBroker" /> is
    ///     added to the <see cref="IServiceCollection" />.
    /// </summary>
    public class RabbitBrokerOptionsConfigurator : IBrokerOptionsConfigurator<RabbitBroker>
    {
        /// <inheritdoc />
        public void Configure(IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddSingleton<IRabbitConnectionFactory, RabbitConnectionFactory>()
                .AddSingletonBrokerBehavior<RabbitRoutingKeyInitializerProducerBehavior>();
        }
    }
}
