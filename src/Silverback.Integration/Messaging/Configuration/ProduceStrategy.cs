// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Contains some helper methods used to build the produce strategies.
    /// </summary>
    public static class ProduceStrategy
    {
        /// <summary>
        ///     Builds an instance of the <see cref="DefaultProduceStrategy" /> that produces the messages to the
        ///     configured message broker.
        /// </summary>
        /// <returns>
        ///     The produce strategy instance.
        /// </returns>
        public static IProduceStrategy Default() => new DefaultProduceStrategy();

        /// <summary>
        ///     Builds an instance of the <see cref="OutboxProduceStrategy" /> that stores the outbound messages in
        ///     the transactional outbox table, for them to be asynchronously produced by the
        ///     <see cref="IOutboxWorker" />.
        /// </summary>
        /// <returns>
        ///     The produce strategy instance.
        /// </returns>
        public static IProduceStrategy Outbox() => new OutboxProduceStrategy();
    }
}
