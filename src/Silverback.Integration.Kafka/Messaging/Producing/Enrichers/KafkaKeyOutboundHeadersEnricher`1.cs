// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     The enricher that sets the message id header according to a value provider function.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be enriched.
/// </typeparam>
public class KafkaKeyOutboundHeadersEnricher<TMessage> : GenericOutboundHeadersEnricher<TMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaKeyOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="valueProvider">
    ///     The header value provider function.
    /// </param>
    public KafkaKeyOutboundHeadersEnricher(Func<TMessage?, object?> valueProvider)
        : base(KafkaMessageHeaders.MessageKey, valueProvider)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaKeyOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="valueProvider">
    ///     The header value provider function.
    /// </param>
    public KafkaKeyOutboundHeadersEnricher(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
        : base(KafkaMessageHeaders.MessageKey, valueProvider)
    {
    }
}
