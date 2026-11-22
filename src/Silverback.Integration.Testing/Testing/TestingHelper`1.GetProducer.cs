// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>GetProducer</c> methods.
/// </content>
public abstract partial class TestingHelper
{
    /// <inheritdoc cref="ITestingHelper.GetProducerForEndpoint" />
    public IProducer GetProducerForEndpoint(string endpointName)
    {
        Check.NotNull(endpointName);

        IProducer? producer = GetConfiguredProducer(endpointName);

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
        GetConfiguredProducer(endpointName)?.EndpointConfiguration;

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
    /// <exception cref="InvalidOperationException">
    ///     The endpoint is a topic pattern or wildcard filter, the matching consumer endpoint subscribes only topic patterns or
    ///     wildcard filters, or the key provider of the consumer's decryption settings doesn't return a key.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///     The decryption settings of the consumer endpoint to mirror aren't supported.
    /// </exception>
    protected abstract IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName);

    // The friendly name takes precedence over the topic name
    private IProducer? GetConfiguredProducer(string endpointName) =>
        _producers?.FirstOrDefault(producer => producer.EndpointConfiguration.FriendlyName == endpointName) ??
        _producers?.FirstOrDefault(producer => producer.EndpointConfiguration.RawName == endpointName);
}
