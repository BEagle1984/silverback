// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Configuration
{
    public static class EndpointsBusConfiguratorExtensions
    {
        public static BusConfigurator Connect(this BusConfigurator configurator,
            Action<EndpointsConfigurationBuilder> endpointsConfigurationAction)
        {
            if (endpointsConfigurationAction == null)
                throw new ArgumentNullException(nameof(endpointsConfigurationAction));

            endpointsConfigurationAction.Invoke(new EndpointsConfigurationBuilder(
                configurator.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                configurator.ServiceProvider.GetRequiredService<IEnumerable<IInboundConnector>>(),
                configurator.ServiceProvider.GetRequiredService<ErrorPolicyBuilder>()));

            configurator.ServiceProvider.GetRequiredService<IBroker>().Connect();

            return configurator;
        }
    }
}
