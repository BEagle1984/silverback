// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    public class RabbitBrokerOptionsConfigurator : IBrokerOptionsConfigurator<RabbitBroker>
    {
        public void Configure(IBrokerOptionsBuilder options) =>
            options.SilverbackBuilder.Services
                .AddSingleton<IRabbitConnectionFactory, RabbitConnectionFactory>()
                .AddSingletonBrokerBehavior<RabbitRoutingKeyInitializerProducerBehavior>();
    }
}