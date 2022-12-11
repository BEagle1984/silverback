// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

/// <summary>
///     Enriches the outbound message (e.g. adding custom headers).
/// </summary>
public interface IOutboundMessageEnricher
{
    /// <summary>
    ///     Enriches the specified message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be enriched.
    /// </param>
    void Enrich(IOutboundEnvelope envelope);
}
