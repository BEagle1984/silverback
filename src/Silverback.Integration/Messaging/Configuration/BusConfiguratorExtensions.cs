// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Configuration
{
    public static class BusConfiguratorExtensions
    {
        /// <summary>
        /// Configures the message broker bindings and starts consuming.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="endpointsConfigurationAction">The inbound/outbound endpoints configuration.</param>
        /// <returns></returns>
        public static IBroker Connect(this BusConfigurator configurator,
            Action<IEndpointsConfigurationBuilder> endpointsConfigurationAction)
        {
            if (endpointsConfigurationAction == null)
                throw new ArgumentNullException(nameof(endpointsConfigurationAction));

            endpointsConfigurationAction.Invoke(new EndpointsConfigurationBuilder(
                configurator.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                configurator.ServiceProvider.GetRequiredService<IEnumerable<IInboundConnector>>(),
                configurator.ServiceProvider.GetRequiredService<ErrorPolicyBuilder>()));

            var broker = configurator.ServiceProvider.GetRequiredService<IBroker>();

            broker.Connect();

            return broker;
        }
    }
}
