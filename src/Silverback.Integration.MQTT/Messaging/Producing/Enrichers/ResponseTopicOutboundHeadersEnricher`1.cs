// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     The enricher that sets the response topic temporary header.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be enriched.
/// </typeparam>
public class ResponseTopicOutboundHeadersEnricher<TMessage> : GenericOutboundHeadersEnricher<TMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseTopicOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="responseTopic">
    ///     The response topic.
    /// </param>
    public ResponseTopicOutboundHeadersEnricher(string? responseTopic)
        : base(MqttMessageHeaders.ResponseTopic, responseTopic)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseTopicOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    public ResponseTopicOutboundHeadersEnricher(Func<TMessage?, string?> responseTopicProvider)
        : base(MqttMessageHeaders.ResponseTopic, responseTopicProvider)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseTopicOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    public ResponseTopicOutboundHeadersEnricher(Func<IOutboundEnvelope<TMessage>, object?> responseTopicProvider)
        : base(MqttMessageHeaders.ResponseTopic, responseTopicProvider)
    {
    }
}
