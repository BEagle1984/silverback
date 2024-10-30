// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <content>
///     Implements the <c>SetCorrelationData</c> methods.
/// </content>
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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData(string? correlationData) =>
        SetCorrelationData(correlationData.ToUtf8Bytes());

    /// <summary>
    ///     Sets the specified correlation data to all produced messages.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData(byte[]? correlationData) =>
        AddMessageEnricher(new CorrelationDataOutboundHeadersEnricher<TMessage>(correlationData));

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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType>(string? correlationData)
        where TMessageChildType : TMessage =>
        SetCorrelationData<TMessageChildType>(correlationData.ToUtf8Bytes());

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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType>(byte[]? correlationData)
        where TMessageChildType : TMessage =>
        AddMessageEnricher(new CorrelationDataOutboundHeadersEnricher<TMessageChildType>(correlationData));

    /// <summary>
    ///     Sets the correlation data to all produced messages, using a provider function to determine the correlation data for each message.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData(Func<TMessage?, string?> correlationDataProvider) =>
        SetCorrelationData(message => correlationDataProvider(message).ToUtf8Bytes());

    /// <summary>
    ///     Sets the correlation data to all produced messages, using a provider function to determine the correlation data for each message.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData(Func<TMessage?, byte[]?> correlationDataProvider)
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundHeadersEnricher<TMessage>(correlationDataProvider));
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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType>(Func<TMessageChildType?, string?> correlationDataProvider)
        where TMessageChildType : TMessage =>
        SetCorrelationData<TMessageChildType>(message => correlationDataProvider.Invoke(message).ToUtf8Bytes());

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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType>(Func<TMessageChildType?, byte[]?> correlationDataProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundHeadersEnricher<TMessageChildType>(correlationDataProvider));
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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData(Func<IOutboundEnvelope<TMessage>, string?> correlationDataProvider) =>
        SetCorrelationData(envelope => correlationDataProvider.Invoke(envelope).ToUtf8Bytes());

    /// <summary>
    ///     Sets the correlation data to all produced messages, using a provider function to determine the correlation data for each message.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData(Func<IOutboundEnvelope<TMessage>, byte[]?> correlationDataProvider)
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundHeadersEnricher<TMessage>(correlationDataProvider));
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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType>(Func<IOutboundEnvelope<TMessageChildType>, string?> correlationDataProvider)
        where TMessageChildType : TMessage =>
        SetCorrelationData<TMessageChildType>(envelope => correlationDataProvider.Invoke(envelope).ToUtf8Bytes());

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
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetCorrelationData<TMessageChildType>(Func<IOutboundEnvelope<TMessageChildType>, byte[]?> correlationDataProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));

        return AddMessageEnricher(new CorrelationDataOutboundHeadersEnricher<TMessageChildType>(correlationDataProvider));
    }
}
