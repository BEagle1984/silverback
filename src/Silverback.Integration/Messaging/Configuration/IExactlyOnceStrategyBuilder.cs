// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="IExactlyOnceStrategy" />.
    /// </summary>
    public interface IExactlyOnceStrategyBuilder
    {
        /// <summary>
        ///     Creates an <see cref="OffsetStoreExactlyOnceStrategy" /> that uses an <see cref="IOffsetStore" /> to
        ///     keep track of the latest processed offsets and guarantee that each message is processed only once.
        /// </summary>
        /// <returns>
        ///     The <see cref="IExactlyOnceStrategyBuilder" /> so that additional calls can be chained.
        /// </returns>
        IExactlyOnceStrategyBuilder StoreOffsets();

        /// <summary>
        ///     Creates a <see cref="LogExactlyOnceStrategy" /> that uses an <see cref="IInboundLog" /> to keep track
        ///     of each processed message and guarantee that each one is processed only once.
        /// </summary>
        /// <returns>
        ///     The <see cref="IExactlyOnceStrategyBuilder" /> so that additional calls can be chained.
        /// </returns>
        IExactlyOnceStrategyBuilder LogMessages();
    }
}
