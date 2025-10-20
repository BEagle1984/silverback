// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     The enricher that sets the correlation data header.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be enriched.
/// </typeparam>
public class CorrelationDataOutboundMessageEnricher<TMessage, TCorrelationData> : IOutboundMessageEnricher
{
    private readonly Func<IOutboundEnvelope<TMessage>, TCorrelationData?> _correlationDataProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CorrelationDataOutboundMessageEnricher{TMessage,TCorrelationData}" /> class.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    public CorrelationDataOutboundMessageEnricher(TCorrelationData? correlationData)
        : this((TMessage? _) => correlationData)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CorrelationDataOutboundMessageEnricher{TMessage,TCorrelationData}" /> class.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    public CorrelationDataOutboundMessageEnricher(Func<TMessage?, TCorrelationData?> correlationDataProvider)
    {
        Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));
        _correlationDataProvider = envelope => correlationDataProvider.Invoke(envelope.Message);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CorrelationDataOutboundMessageEnricher{TMessage,TCorrelationData}" /> class.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    public CorrelationDataOutboundMessageEnricher(Func<IOutboundEnvelope<TMessage>, TCorrelationData?> correlationDataProvider)
    {
        _correlationDataProvider = Check.NotNull(correlationDataProvider, nameof(correlationDataProvider));
    }

    /// <inheritdoc cref="IOutboundMessageEnricher.Enrich" />
    public void Enrich(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        if (envelope is not IMqttOutboundEnvelope<TMessage, TCorrelationData> mqttEnvelope)
            return;

        mqttEnvelope.SetCorrelationData(_correlationDataProvider.Invoke(mqttEnvelope));
    }
}
