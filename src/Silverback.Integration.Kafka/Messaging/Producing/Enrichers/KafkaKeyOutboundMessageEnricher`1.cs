// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     The enricher that sets the message id header according to a value provider function.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be enriched.
/// </typeparam>
public class KafkaKeyOutboundMessageEnricher<TMessage> : IOutboundMessageEnricher
{
    private readonly Func<IOutboundEnvelope<TMessage>, object?> _keyProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaKeyOutboundMessageEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="keyProvider">
    ///     The key provider function.
    /// </param>
    public KafkaKeyOutboundMessageEnricher(Func<TMessage?, object?> keyProvider)
    {
        _keyProvider = envelope => keyProvider.Invoke(envelope.Message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaKeyOutboundMessageEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="valueProvider">
    ///     The header value provider function.
    /// </param>
    public KafkaKeyOutboundMessageEnricher(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
    {
        _keyProvider = valueProvider;
    }

    /// <inheritdoc cref="IOutboundMessageEnricher.Enrich" />
    public void Enrich(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is not IKafkaOutboundEnvelope<TMessage> kafkaEnvelope)
            return;

        kafkaEnvelope.SetKey(_keyProvider.Invoke(kafkaEnvelope));
    }
}
