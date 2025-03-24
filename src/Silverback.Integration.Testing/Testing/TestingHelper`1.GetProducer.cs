// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>GetProducer</c> methods.
/// </content>
public abstract partial class TestingHelper
{
    /// <inheritdoc cref="ITestingHelper.GetProducerForEndpoint" />
    public IProducer GetProducerForEndpoint(string endpointName)
    {
        IProducer? producer = _producers?.FirstOrDefault(
            producer => producer.EndpointConfiguration.RawName == endpointName ||
                        producer.EndpointConfiguration.FriendlyName == endpointName);

        if (producer != null)
            return producer;

        IProducer? newProducer = null;

        if (_consumers != null)
            newProducer = GetProducerForConsumer(_consumers, endpointName);

        if (newProducer == null)
            throw new InvalidOperationException($"No producer and no consumer found for endpoint '{endpointName}'.");

        return newProducer;
    }

    /// <summary>
    ///     Returns the <see cref="ProducerEndpointConfiguration" /> for the specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpointConfiguration" />.
    /// </returns>
    protected ProducerEndpointConfiguration? GetProducerEndpointConfiguration(string endpointName) =>
        _producers?
            .Select(producer => producer.EndpointConfiguration)
            .FirstOrDefault(configuration => configuration.RawName == endpointName || configuration.FriendlyName == endpointName);

    /// <summary>
    ///     Gets an existing producer for the endpoint consumed by the specified consumer or initializes a new one mirroring the
    ///     consumer configuration.
    /// </summary>
    /// <param name="consumers">
    ///     The existing consumers.
    /// </param>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    protected abstract IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName);
}
