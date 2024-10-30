// Copyright (c) 2024 Sergio Aquilini
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
public class CorrelationDataOutboundHeadersEnricher<TMessage> : GenericOutboundHeadersEnricher<TMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CorrelationDataOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    public CorrelationDataOutboundHeadersEnricher(byte[]? correlationData)
        : base(MqttMessageHeaders.CorrelationData, correlationData.ToBase64String())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CorrelationDataOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    public CorrelationDataOutboundHeadersEnricher(Func<TMessage?, byte[]?> correlationDataProvider)
        : base(MqttMessageHeaders.CorrelationData, message => correlationDataProvider.Invoke(message)?.ToBase64String())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CorrelationDataOutboundHeadersEnricher{TMessage}" /> class.
    /// </summary>
    /// <param name="correlationDataProvider">
    ///     The correlation data provider function.
    /// </param>
    public CorrelationDataOutboundHeadersEnricher(Func<IOutboundEnvelope<TMessage>, byte[]?> correlationDataProvider)
        : base(MqttMessageHeaders.CorrelationData, envelope => correlationDataProvider.Invoke(envelope)?.ToBase64String())
    {
    }
}
