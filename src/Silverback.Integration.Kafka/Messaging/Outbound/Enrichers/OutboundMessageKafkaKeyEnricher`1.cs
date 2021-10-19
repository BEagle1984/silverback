// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Enrichers
{
    /// <summary>
    ///     A generic enricher that sets the kafka key according to a value provider function.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be enriched.
    /// </typeparam>
    public class OutboundMessageKafkaKeyEnricher<TMessage> : GenericOutboundHeadersEnricher<TMessage>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundMessageKafkaKeyEnricher{TMessage}" /> class.
        /// </summary>
        /// <param name="valueProvider">
        ///     The kafka key value provider function.
        /// </param>
        public OutboundMessageKafkaKeyEnricher(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
            : base(
                KafkaMessageHeaders.KafkaMessageKey,
                envelope => Check.NotNull(valueProvider, nameof(valueProvider)).Invoke(envelope))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundMessageKafkaKeyEnricher{TMessage}" /> class.
        /// </summary>
        /// <param name="valueProvider">
        ///     The kafka key value provider function.
        /// </param>
        public OutboundMessageKafkaKeyEnricher(Func<TMessage?, object?> valueProvider)
            : base(
                KafkaMessageHeaders.KafkaMessageKey,
                envelope => Check.NotNull(valueProvider, nameof(valueProvider)).Invoke(envelope))
        {
        }
    }
}
