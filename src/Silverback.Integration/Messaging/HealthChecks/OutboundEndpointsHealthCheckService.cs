// Copyright (c) 2020 Sergio Aquilini
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
        private readonly IBrokerCollection _brokerCollection;

        public OutboundEndpointsHealthCheckService(
            IOutboundRoutingConfiguration outboundRoutingConfiguration,
            IBrokerCollection brokerCollection)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration ??
                                            throw new ArgumentNullException(nameof(outboundRoutingConfiguration));
            _brokerCollection = brokerCollection ?? throw new ArgumentNullException(nameof(brokerCollection));
        }

        public async Task<IEnumerable<EndpointCheckResult>> PingAllEndpoints()
        {
            if (!_brokerCollection.All(broker => broker.IsConnected))
                return Enumerable.Empty<EndpointCheckResult>();

            var tasks = _outboundRoutingConfiguration.Routes
                .SelectMany(route => route.Router.Endpoints
                    .Select(async endpoint =>
                    {
                        try
                        {
                            await _brokerCollection.GetProducer(endpoint).ProduceAsync(PingMessage.New());
                            return new EndpointCheckResult(endpoint.Name, true);
                        }
                        catch (Exception ex)
                        {
                            return new EndpointCheckResult(endpoint.Name, false,
                                $"[{ex.GetType().FullName}] {ex.Message}");
                        }
                    }));

            return (await Task.WhenAll(tasks));
        }
    }
}