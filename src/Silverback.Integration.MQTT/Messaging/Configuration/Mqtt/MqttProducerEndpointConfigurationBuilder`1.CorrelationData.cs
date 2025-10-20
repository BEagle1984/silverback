// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <content>
///     Implements the <c>SetCorrelationData</c> methods.
/// </content>
// TODO: Check tests (data types)
public partial class MqttProducerEndpointConfigurationBuilder<TMessage>
{
    /// <summary>
    ///     Sets the specified correlation data to all produced messages.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TCorrelationData>(TCorrelationData? correlationData) =>
        AddMessageEnricher(new CorrelationDataOutboundMessageEnricher<TMessage, TCorrelationData>(correlationData));

    /// <summary>
    ///     Sets the specified correlation data to all produced messages of the specified child type.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType, TCorrelationData>(TCorrelationData? correlationData)
        where TMessageChildType : TMessage =>
        AddMessageEnricher(new CorrelationDataOutboundMessageEnricher<TMessageChildType, TCorrelationData>(correlationData));

    /// <summary>
    ///     Sets the correlation data to all produced messages, using a provider function to determine the correlation data for each message.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TCorrelationData>(Func<TMessage?, TCorrelationData?> correlationDataProvider)
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundMessageEnricher<TMessage, TCorrelationData>(correlationDataProvider));
    }

    /// <summary>
    ///     Sets the correlation data to all produced messages of the specified child type, using a provider function to determine the
    ///     correlation data for each message.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType, TCorrelationData>(Func<TMessageChildType?, TCorrelationData?> correlationDataProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundMessageEnricher<TMessageChildType, TCorrelationData>(correlationDataProvider));
    }

    /// <summary>
    ///     Sets the correlation data to all produced messages, using a provider function to determine the correlation data for each message.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TCorrelationData>(Func<IOutboundEnvelope<TMessage>, TCorrelationData?> correlationDataProvider)
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundMessageEnricher<TMessage, TCorrelationData>(correlationDataProvider));
    }

    /// <summary>
    ///     Sets the correlation data to all produced messages of the specified child type, using a provider function to determine the
    ///     correlation data for each message.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType, TCorrelationData>(Func<IOutboundEnvelope<TMessageChildType>, TCorrelationData?> correlationDataProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundMessageEnricher<TMessageChildType, TCorrelationData>(correlationDataProvider));
    }
}
