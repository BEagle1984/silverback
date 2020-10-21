// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Contains some helper methods used to build the exactly-once strategies.
    /// </summary>
    public static class ExactlyOnceStrategy
    {
        /// <summary>
        ///     Builds an instance of the <see cref="OffsetStoreExactlyOnceStrategy" /> that uses an
        ///     <see cref="IOffsetStore" /> to keep track of the latest processed offsets and guarantee that each
        ///     message is processed only once.
        /// </summary>
        /// <returns>
        ///     The exactly-once  strategy instance.
        /// </returns>
        public static IExactlyOnceStrategy OffsetStore() => new OffsetStoreExactlyOnceStrategy();

        /// <summary>
        ///     Builds an instance of the <see cref="LogExactlyOnceStrategy" /> that uses an
        ///     <see cref="IInboundLog" /> to keep track of each processed message and guarantee that each one is
        ///     processed only once.
        /// </summary>
        /// <returns>
        ///     The exactly-once strategy instance.
        /// </returns>
        public static IExactlyOnceStrategy Log() => new LogExactlyOnceStrategy();
    }
}
