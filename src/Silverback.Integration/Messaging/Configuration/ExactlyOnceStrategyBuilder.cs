// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="IExactlyOnceStrategy" />.
/// </summary>
public class ExactlyOnceStrategyBuilder
{
    private IExactlyOnceStrategy? _strategy;

    /// <summary>
    ///     Creates an <see cref="OffsetStoreExactlyOnceStrategy" /> that uses an <see cref="IOffsetStore" /> to
    ///     keep track of the latest processed offsets and guarantee that each message is processed only once.
    /// </summary>
    /// <returns>
    ///     The <see cref="ExactlyOnceStrategyBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ExactlyOnceStrategyBuilder StoreOffsets()
    {
        if (_strategy != null)
            throw new InvalidOperationException("Cannot use multiple IExactlyOnceStrategy.");

        _strategy = new OffsetStoreExactlyOnceStrategy();
        return this;
    }

    /// <summary>
    ///     Creates a <see cref="LogExactlyOnceStrategy" /> that uses an <see cref="IInboundLog" /> to keep track
    ///     of each processed message and guarantee that each one is processed only once.
    /// </summary>
    /// <returns>
    ///     The <see cref="ExactlyOnceStrategyBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ExactlyOnceStrategyBuilder LogMessages()
    {
        if (_strategy != null)
            throw new InvalidOperationException("Cannot use multiple IExactlyOnceStrategy.");

        _strategy = new LogExactlyOnceStrategy();
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IExactlyOnceStrategy" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IExactlyOnceStrategy" />.
    /// </returns>
    public IExactlyOnceStrategy Build()
    {
        if (_strategy == null)
            throw new InvalidOperationException("No strategy was specified.");

        return _strategy;
    }
}
