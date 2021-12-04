// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Testing;

/// <inheritdoc cref="IIntegrationSpy" />
public class IntegrationSpy : IIntegrationSpy
{
    private readonly List<IOutboundEnvelope> _outboundEnvelopes = new();

    private readonly List<IRawOutboundEnvelope> _rawOutboundEnvelopes = new();

    private readonly List<IRawInboundEnvelope> _rawInboundEnvelopes = new();

    private readonly List<IInboundEnvelope> _inboundEnvelopes = new();

    /// <inheritdoc cref="IIntegrationSpy.OutboundEnvelopes" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
    public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes;

    /// <inheritdoc cref="IIntegrationSpy.RawOutboundEnvelopes" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
    public IReadOnlyList<IRawOutboundEnvelope> RawOutboundEnvelopes => _rawOutboundEnvelopes;

    /// <inheritdoc cref="IIntegrationSpy.RawInboundEnvelopes" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
    public IReadOnlyList<IRawInboundEnvelope> RawInboundEnvelopes => _rawInboundEnvelopes;

    /// <inheritdoc cref="IIntegrationSpy.InboundEnvelopes" />
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
    public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes;

    /// <summary>
    ///     Adds an item to the <see cref="OutboundEnvelopes" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" /> to add.
    /// </param>
    public void AddOutboundEnvelope(IOutboundEnvelope envelope)
    {
        lock (_outboundEnvelopes)
        {
            _outboundEnvelopes.Add(envelope);
        }
    }

    /// <summary>
    ///     Adds an item to the <see cref="RawOutboundEnvelope" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IRawOutboundEnvelope" /> to add.
    /// </param>
    public void AddRawOutboundEnvelope(IRawOutboundEnvelope envelope)
    {
        lock (_rawOutboundEnvelopes)
        {
            _rawOutboundEnvelopes.Add(envelope);
        }
    }

    /// <summary>
    ///     Adds an item to the <see cref="RawInboundEnvelope" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IRawInboundEnvelope" /> to add.
    /// </param>
    public void AddRawInboundEnvelope(IRawInboundEnvelope envelope)
    {
        lock (_rawInboundEnvelopes)
        {
            _rawInboundEnvelopes.Add(envelope);
        }
    }

    /// <summary>
    ///     Adds an item to the <see cref="InboundEnvelopes" />.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IInboundEnvelope" /> to add.
    /// </param>
    public void AddInboundEnvelope(IInboundEnvelope envelope)
    {
        lock (_inboundEnvelopes)
        {
            _inboundEnvelopes.Add(envelope);
        }
    }
}
