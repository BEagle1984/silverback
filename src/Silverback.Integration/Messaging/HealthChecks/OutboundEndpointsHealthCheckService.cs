// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.HealthChecks
{
    /// <inheritdoc cref="IOutboundEndpointsHealthCheckService" />
    public class OutboundEndpointsHealthCheckService : IOutboundEndpointsHealthCheckService
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;

        private readonly IBrokerCollection _brokerCollection;

        private readonly IServiceProvider _serviceProvider;

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
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        public OutboundEndpointsHealthCheckService(
            IOutboundRoutingConfiguration outboundRoutingConfiguration,
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider)
        {
            _outboundRoutingConfiguration = Check.NotNull(
                outboundRoutingConfiguration,
                nameof(outboundRoutingConfiguration));
            _brokerCollection = Check.NotNull(brokerCollection, nameof(brokerCollection));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        /// <inheritdoc cref="IOutboundEndpointsHealthCheckService.PingAllEndpointsAsync" />
        [SuppressMessage("", "CA1031", Justification = "Exception is returned")]
        public async Task<IReadOnlyCollection<EndpointCheckResult>> PingAllEndpointsAsync()
        {
            if (!_brokerCollection.All(broker => broker.IsConnected))
                return Array.Empty<EndpointCheckResult>();

            var tasks =
                _outboundRoutingConfiguration.Routes.SelectMany(
                    route =>
                        route.GetOutboundRouter(_serviceProvider).Endpoints.Select(PingEndpointAsync));

            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        [SuppressMessage("", "CA1031", Justification = "Exception reported in the result.")]
        private async Task<EndpointCheckResult> PingEndpointAsync(IProducerEndpoint endpoint)
        {
            try
            {
                await _brokerCollection.GetProducer(endpoint).ProduceAsync(PingMessage.New()).ConfigureAwait(false);
                return new EndpointCheckResult(endpoint.DisplayName, true);
            }
            catch (Exception ex)
            {
                return new EndpointCheckResult(
                    endpoint.DisplayName,
                    false,
                    $"[{ex.GetType().FullName}] {ex.Message}");
            }
        }
    }
}
