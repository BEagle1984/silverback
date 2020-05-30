// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <inheritdoc cref="IOutboundEndpointsHealthCheckService" />
    public class OutboundEndpointsHealthCheckService : IOutboundEndpointsHealthCheckService
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;

        private readonly IBrokerCollection _brokerCollection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundEndpointsHealthCheckService" /> class.
        /// </summary>
        /// <param name="outboundRoutingConfiguration">
        ///     The <see cref="IOutboundRoutingConfiguration" /> to be used to retrieve the list of outbound
        ///     endpoints.
        /// </param>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        public OutboundEndpointsHealthCheckService(
            IOutboundRoutingConfiguration outboundRoutingConfiguration,
            IBrokerCollection brokerCollection)
        {
            _outboundRoutingConfiguration = Check.NotNull(
                outboundRoutingConfiguration,
                nameof(outboundRoutingConfiguration));
            _brokerCollection = Check.NotNull(brokerCollection, nameof(brokerCollection));
        }

        /// <inheritdoc cref="IOutboundEndpointsHealthCheckService.PingAllEndpoints" />
        [SuppressMessage("", "CA1031", Justification = "Exception is returned")]
        public async Task<IReadOnlyCollection<EndpointCheckResult>> PingAllEndpoints()
        {
            if (!_brokerCollection.All(broker => broker.IsConnected))
                return Array.Empty<EndpointCheckResult>();

            var tasks =
                _outboundRoutingConfiguration.Routes.SelectMany(
                    route =>
                        route.Router.Endpoints.Select(PingEndpoint));

            return await Task.WhenAll(tasks);
        }

        [SuppressMessage("", "CA1031", Justification = "Exception reported in the result.")]
        private async Task<EndpointCheckResult> PingEndpoint(IProducerEndpoint endpoint)
        {
            try
            {
                await _brokerCollection.GetProducer(endpoint).ProduceAsync(PingMessage.New());
                return new EndpointCheckResult(endpoint.Name, true);
            }
            catch (Exception ex)
            {
                return new EndpointCheckResult(
                    endpoint.Name,
                    false,
                    $"[{ex.GetType().FullName}] {ex.Message}");
            }
        }
    }
}
