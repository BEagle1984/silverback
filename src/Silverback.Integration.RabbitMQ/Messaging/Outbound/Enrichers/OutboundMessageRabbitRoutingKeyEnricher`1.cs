// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Enrichers
{
    /// <summary>
    ///     A generic enricher that sets the routing key according to a value provider function.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be enriched.
    /// </typeparam>
    public class OutboundMessageRabbitRoutingKeyEnricher<TMessage> : GenericOutboundHeadersEnricher<TMessage>
        where TMessage : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundMessageRabbitRoutingKeyEnricher{TMessage}" /> class.
        /// </summary>
        /// <param name="valueProvider">
        ///     The kafka key value provider function.
        /// </param>
        public OutboundMessageRabbitRoutingKeyEnricher(
            Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
            : base(
                RabbitMessageHeaders.RoutingKey,
                envelope => Check.NotNull(valueProvider, nameof(valueProvider)).Invoke(envelope))
        {
        }
    }
}
