// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     Enriches the outbound message being moved.
/// </summary>
public interface IMovePolicyMessageEnricher
{
    /// <summary>
    ///     Enriches the specified message.
    /// </summary>
    /// <param name="inboundEnvelope">
    ///     The envelope containing the message which failed to be processed.
    /// </param>
    /// <param name="outboundEnvelope">
    ///     The envelope containing the message to be enriched.
    /// </param>
    /// <param name="exception">
    ///     The exception thrown during the message processing.
    /// </param>
    void Enrich(
        IRawInboundEnvelope inboundEnvelope,
        IOutboundEnvelope outboundEnvelope,
        Exception exception);
}
