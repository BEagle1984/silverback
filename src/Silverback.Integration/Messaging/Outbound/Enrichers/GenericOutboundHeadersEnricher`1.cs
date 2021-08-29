// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Enrichers
{
    /// <summary>
    ///     A generic enricher that adds a message header according to a static name/value pair or a provider
    ///     function.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be enriched.
    /// </typeparam>
    public class GenericOutboundHeadersEnricher<TMessage> : IOutboundMessageEnricher
        where TMessage : class
    {
        private readonly string _name;

        private readonly Func<IOutboundEnvelope<TMessage>, object?> _valueProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GenericOutboundHeadersEnricher{TMessage}" /> class.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="value">
        ///     The header value.
        /// </param>
        public GenericOutboundHeadersEnricher(string name, object? value)
            : this(name, _ => value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GenericOutboundHeadersEnricher{TMessage}" /> class.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="valueProvider">
        ///     The header value provider function.
        /// </param>
        public GenericOutboundHeadersEnricher(
            string name,
            Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
        {
            _name = Check.NotNull(name, nameof(name));
            _valueProvider = Check.NotNull(valueProvider, nameof(valueProvider));
        }

        /// <inheritdoc cref="IOutboundMessageEnricher.Enrich" />
        public void Enrich(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (envelope is not IOutboundEnvelope<TMessage> typedEnvelope)
                return;

            envelope.Headers.AddOrReplace(_name, _valueProvider.Invoke(typedEnvelope));
        }
    }
}
