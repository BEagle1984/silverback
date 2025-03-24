// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <content>
///     Implements the <c>SetResponseTopic</c> methods.
/// </content>
public partial class MqttProducerEndpointConfigurationBuilder<TMessage>
{
    /// <summary>
    ///     Sets the specified response topic to all produced messages.
    /// </summary>
    /// <param name="responseTopic">
    ///     The response topic.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetResponseTopic(string? responseTopic) =>
        AddMessageEnricher(new ResponseTopicOutboundHeadersEnricher<TMessage>(responseTopic));

    /// <summary>
    ///     Sets the specified response topic to all produced messages of the specified child type.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="responseTopic">
    ///     The response topic.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetResponseTopic<TMessageChildType>(string responseTopic)
        where TMessageChildType : TMessage =>
        AddMessageEnricher(new ResponseTopicOutboundHeadersEnricher<TMessageChildType>(responseTopic));

    /// <summary>
    ///     Sets the response topic to all produced messages, using a provider function to determine the response topic for each message.
    /// </summary>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetResponseTopic(Func<TMessage?, string?> responseTopicProvider)
    {
        Check.NotNull(responseTopicProvider, nameof(responseTopicProvider));

        return AddMessageEnricher(new ResponseTopicOutboundHeadersEnricher<TMessage>(responseTopicProvider));
    }

    /// <summary>
    ///     Sets the response topic to all produced messages of the specified child type, using a provider function to determine the
    ///     response topic for each message.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetResponseTopic<TMessageChildType>(Func<TMessageChildType?, string?> responseTopicProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(responseTopicProvider, nameof(responseTopicProvider));

        return AddMessageEnricher(new ResponseTopicOutboundHeadersEnricher<TMessageChildType>(responseTopicProvider));
    }

    /// <summary>
    ///     Sets the response topic to all produced messages, using a provider function to determine the response topic for each message.
    /// </summary>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetResponseTopic(Func<IOutboundEnvelope<TMessage>, string?> responseTopicProvider)
    {
        Check.NotNull(responseTopicProvider, nameof(responseTopicProvider));

        return AddMessageEnricher(new ResponseTopicOutboundHeadersEnricher<TMessage>(responseTopicProvider));
    }

    /// <summary>
    ///     Sets the response topic to all produced messages of the specified child type, using a provider function to determine the
    ///     response topic for each message.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public MqttProducerEndpointConfigurationBuilder<TMessage> SetResponseTopic<TMessageChildType>(Func<IOutboundEnvelope<TMessageChildType>, string?> responseTopicProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(responseTopicProvider, nameof(responseTopicProvider));

        return AddMessageEnricher(new ResponseTopicOutboundHeadersEnricher<TMessageChildType>(responseTopicProvider));
    }
}
