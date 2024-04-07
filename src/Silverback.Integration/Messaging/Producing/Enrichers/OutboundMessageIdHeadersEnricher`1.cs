// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     A generic enricher that sets the message id header according to a value provider function.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be enriched.
/// </typeparam>
public class OutboundMessageIdHeadersEnricher<TMessage> : GenericOutboundHeadersEnricher<TMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboundMessageIdHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="valueProvider">
    ///     The header value provider function.
    /// </param>
    public OutboundMessageIdHeadersEnricher(Func<TMessage?, object?> valueProvider)
        : base(DefaultMessageHeaders.MessageId, valueProvider)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboundMessageIdHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="valueProvider">
    ///     The header value provider function.
    /// </param>
    public OutboundMessageIdHeadersEnricher(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
        : base(DefaultMessageHeaders.MessageId, valueProvider)
    {
    }
}
