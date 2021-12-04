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

namespace Silverback.Messaging.HealthChecks;

/// <inheritdoc cref="IProducersHealthCheckService" />
public class ProducersHealthCheckService : IProducersHealthCheckService
{
    private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;

    private readonly IBrokerCollection _brokerCollection;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducersHealthCheckService" /> class.
    /// </summary>
    /// <param name="outboundRoutingConfiguration">
    ///     The <see cref="IOutboundRoutingConfiguration" /> to be used to retrieve the list of outbound endpoints.
    /// </param>
    /// <param name="brokerCollection">
    ///     The collection containing the available brokers.
    /// </param>
    public ProducersHealthCheckService(IOutboundRoutingConfiguration outboundRoutingConfiguration, IBrokerCollection brokerCollection)
    {
        _outboundRoutingConfiguration = Check.NotNull(outboundRoutingConfiguration, nameof(outboundRoutingConfiguration));
        _brokerCollection = Check.NotNull(brokerCollection, nameof(brokerCollection));
    }

    /// <inheritdoc cref="IProducersHealthCheckService.SendPingMessagesAsync" />
    [SuppressMessage("", "CA1031", Justification = "Exception is returned")]
    public async Task<IReadOnlyCollection<EndpointCheckResult>> SendPingMessagesAsync()
    {
        if (!_brokerCollection.All(broker => broker.IsConnected))
            return Array.Empty<EndpointCheckResult>();

        IEnumerable<Task<EndpointCheckResult>> tasks =
            _outboundRoutingConfiguration.Routes
                .Select(route => route.ProducerConfiguration)
                .Select(PingEndpointAsync);

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    [SuppressMessage("", "CA1031", Justification = "Exception reported in the result.")]
    private async Task<EndpointCheckResult> PingEndpointAsync(ProducerConfiguration producerConfiguration)
    {
        try
        {
            await _brokerCollection.GetProducer(producerConfiguration).ProduceAsync(PingMessage.New()).ConfigureAwait(false);
            return new EndpointCheckResult(producerConfiguration.DisplayName, true);
        }
        catch (Exception ex)
        {
            return new EndpointCheckResult(
                producerConfiguration.DisplayName,
                false,
                $"[{ex.GetType().FullName}] {ex.Message}");
        }
    }
}
