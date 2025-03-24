// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Testing;

/// <summary>
///     Monitors and collects all outbound and inbound messages.
/// </summary>
public interface IIntegrationSpy
{
    /// <summary>
    ///     Gets the list of <see cref="IOutboundEnvelope" /> corresponding to all the outbound messages.
    /// </summary>
    /// <remarks>
    ///     The messages produced via <c>RawProduce</c> or <c>RawProduceAsync</c> will not go through the Silverback
    ///     pipeline and will therefore not show up in this collection.
    /// </remarks>
    IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes { get; }

    /// <summary>
    ///     Gets the list of <see cref="IOutboundEnvelope" /> corresponding to all the actual outbound messages
    ///     being produced (these may differ from the <see cref="OutboundEnvelopes" />, e.g. if chunking is applied).
    /// </summary>
    /// <remarks>
    ///     The messages produced via <c>RawProduce</c> or <c>RawProduceAsync</c> will not go through the Silverback
    ///     pipeline and will therefore not show up in this collection.
    /// </remarks>
    IReadOnlyList<IOutboundEnvelope> RawOutboundEnvelopes { get; }

    /// <summary>
    ///     Gets the list of <see cref="IRawInboundEnvelope" /> corresponding to all the inbound messages, before
    ///     they are even processed by the consumer pipeline.
    /// </summary>
    IReadOnlyList<IRawInboundEnvelope> RawInboundEnvelopes { get; }

    /// <summary>
    ///     Gets the list of <see cref="IInboundEnvelope" /> corresponding to all the inbound messages that have been
    ///     processed by the consumer pipeline (except the ones that couldn't be deserialized, e.g. the ones with an
    ///     empty body).
    /// </summary>
    IReadOnlyList<IInboundEnvelope> InboundEnvelopes { get; }
}
