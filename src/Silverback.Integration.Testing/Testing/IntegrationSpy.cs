// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Testing;

/// <inheritdoc cref="IIntegrationSpy" />
public class IntegrationSpy : IIntegrationSpy
{
    private readonly ConcurrentQueue<IOutboundEnvelope> _outboundEnvelopes = new();

    private readonly ConcurrentQueue<IOutboundEnvelope> _rawOutboundEnvelopes = new();

    private readonly ConcurrentQueue<IRawInboundEnvelope> _rawInboundEnvelopes = new();

    private readonly ConcurrentQueue<IInboundEnvelope> _inboundEnvelopes = new();

    /// <inheritdoc cref="IIntegrationSpy.OutboundEnvelopes" />
    public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => [.. _outboundEnvelopes];

    /// <inheritdoc cref="IIntegrationSpy.RawOutboundEnvelopes" />
    public IReadOnlyList<IOutboundEnvelope> RawOutboundEnvelopes => [.. _rawOutboundEnvelopes];

    /// <inheritdoc cref="IIntegrationSpy.RawInboundEnvelopes" />
    public IReadOnlyList<IRawInboundEnvelope> RawInboundEnvelopes => [.. _rawInboundEnvelopes];

    /// <inheritdoc cref="IIntegrationSpy.InboundEnvelopes" />
    public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => [.. _inboundEnvelopes];

    /// <summary>
    ///     Adds an item to the <see cref="OutboundEnvelopes" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" /> to add.
    /// </param>
    public void AddOutboundEnvelope(IOutboundEnvelope envelope) => _outboundEnvelopes.Enqueue(envelope);

    /// <summary>
    ///     Adds an item to the <see cref="RawOutboundEnvelopes" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" /> to add.
    /// </param>
    public void AddRawOutboundEnvelope(IOutboundEnvelope envelope) => _rawOutboundEnvelopes.Enqueue(envelope);

    /// <summary>
    ///     Adds an item to the <see cref="RawInboundEnvelope" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IRawInboundEnvelope" /> to add.
    /// </param>
    public void AddRawInboundEnvelope(IRawInboundEnvelope envelope) => _rawInboundEnvelopes.Enqueue(envelope);

    /// <summary>
    ///     Adds an item to the <see cref="InboundEnvelopes" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IInboundEnvelope" /> to add.
    /// </param>
    public void AddInboundEnvelope(IInboundEnvelope envelope) => _inboundEnvelopes.Enqueue(envelope);
}
