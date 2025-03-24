// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Dynamically resolves the destination endpoint (e.g. the target topic and partition) for each message being produced.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message being produced.
/// </typeparam>
/// <typeparam name="TEndpoint">
///     The type of the endpoint being resolved.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the endpoint configuration.
/// </typeparam>
public abstract record DynamicProducerEndpointResolver<TMessage, TEndpoint, TConfiguration> : IDynamicProducerEndpointResolver<TEndpoint>
    where TMessage : class
    where TEndpoint : ProducerEndpoint
    where TConfiguration : ProducerEndpointConfiguration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicProducerEndpointResolver{TMessage, TEndpoint, TConfiguration}" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The raw endpoint name that can be used as <see cref="EndpointConfiguration.RawName" />.
    /// </param>
    protected DynamicProducerEndpointResolver(string rawName)
    {
        RawName = Check.NotNullOrEmpty(rawName, nameof(rawName));
    }

    /// <inheritdoc cref="IProducerEndpointResolver.RawName" />
    public string RawName { get; }

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public ProducerEndpoint GetEndpoint(IOutboundEnvelope envelope) =>
        GetEndpoint(envelope, (TConfiguration)Check.NotNull(envelope, nameof(envelope)).EndpointConfiguration);

    /// <inheritdoc cref="IProducerEndpointResolver.GetEndpoint" />
    public TEndpoint GetEndpoint(IOutboundEnvelope envelope, TConfiguration configuration)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is IOutboundEnvelope<TMessage> castedEnvelope)
            return GetEndpointCore(castedEnvelope, configuration);

        if (envelope.Headers.TryGetValue(DefaultMessageHeaders.SerializedEndpoint, out string? serializedEndpoint) &&
            serializedEndpoint != null)
        {
            return DeserializeEndpoint(serializedEndpoint, configuration);
        }

        throw new InvalidOperationException($"The envelope must be of type {typeof(IOutboundEnvelope<TMessage>).FullName}.");
    }

    /// <inheritdoc cref="IDynamicProducerEndpointResolver.GetSerializedEndpoint" />
    public string GetSerializedEndpoint(IOutboundEnvelope envelope) =>
        SerializeEndpoint((TEndpoint)GetEndpoint(envelope));

    /// <summary>
    ///     Gets the computed actual destination endpoint for the message being produced.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ProducerEndpoint" /> for the specified message.
    /// </returns>
    protected abstract TEndpoint GetEndpointCore(IOutboundEnvelope<TMessage> envelope, TConfiguration configuration);

    /// <summary>
    ///     Serializes the endpoint to a string.
    /// </summary>
    /// <param name="endpoint">
    ///     The endpoint to be serialized.
    /// </param>
    /// <returns>
    ///     The serialized endpoint.
    /// </returns>
    protected abstract string SerializeEndpoint(TEndpoint endpoint);

    /// <summary>
    ///     Deserializes the endpoint from a string.
    /// </summary>
    /// <param name="serializedEndpoint">
    ///     The serialized endpoint.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    /// <returns>
    ///     The deserialized endpoint.
    /// </returns>
    protected abstract TEndpoint DeserializeEndpoint(string serializedEndpoint, TConfiguration configuration);
}
