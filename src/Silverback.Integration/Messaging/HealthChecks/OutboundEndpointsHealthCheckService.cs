// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.HealthChecks
{
    public class OutboundEndpointsHealthCheckService : IOutboundEndpointsHealthCheckService
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;
        private readonly IBroker _broker;

        public OutboundEndpointsHealthCheckService(IOutboundRoutingConfiguration outboundRoutingConfiguration, IBroker broker)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration ?? throw new ArgumentNullException(nameof(outboundRoutingConfiguration));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        }

        public async Task<IEnumerable<EndpointCheckResult>> PingAllEndpoints()
        {
            if (!_broker.IsConnected)
                return Enumerable.Empty<EndpointCheckResult>();

            var tasks = _outboundRoutingConfiguration.Routes
                .Select(async route =>
                {
                    try
                    {
                        await _broker.GetProducer(route.DestinationEndpoint).ProduceAsync(PingMessage.New());
                        return new EndpointCheckResult(route.DestinationEndpoint.Name, true);
                    }
                    catch (Exception ex)
                    {
                        return new EndpointCheckResult(route.DestinationEndpoint.Name, false,
                            $"[{ex.GetType().FullName}] {ex.Message}");
                    }
                });

            return await Task.WhenAll(tasks);
        }
    }
}