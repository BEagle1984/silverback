// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     The enricher that sets the response topic temporary header.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be enriched.
/// </typeparam>
public class ResponseTopicOutboundMessageEnricher<TMessage> : IOutboundMessageEnricher
{
    private readonly Func<IOutboundEnvelope<TMessage>, string?> _responseTopicProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseTopicOutboundMessageEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="responseTopic">
    ///     The response topic.
    /// </param>
    public ResponseTopicOutboundMessageEnricher(string? responseTopic)
        : this((TMessage? _) => responseTopic)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseTopicOutboundMessageEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    public ResponseTopicOutboundMessageEnricher(Func<TMessage?, string?> responseTopicProvider)
    {
        Check.NotNull(responseTopicProvider, nameof(responseTopicProvider));
        _responseTopicProvider = envelope => responseTopicProvider.Invoke(envelope.Message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseTopicOutboundMessageEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="responseTopicProvider">
    ///     The response topic provider function.
    /// </param>
    public ResponseTopicOutboundMessageEnricher(Func<IOutboundEnvelope<TMessage>, string?> responseTopicProvider)
    {
        _responseTopicProvider = Check.NotNull(responseTopicProvider, nameof(responseTopicProvider));
    }

    /// <inheritdoc cref="IOutboundMessageEnricher.Enrich" />
    public void Enrich(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is not IMqttOutboundEnvelope<TMessage> mqttEnvelope)
            return;

        mqttEnvelope.SetResponseTopic(_responseTopicProvider.Invoke(mqttEnvelope));
    }
}
