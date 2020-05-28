// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors.Behaviors
{
    /// <summary>
    ///     Contains the sort index constants of the default <see cref="IBehavior" /> added by
    ///     Silverback.Integration.
    /// </summary>
    public static class IntegrationBehaviorsSortIndexes
    {
        /// <summary>
        ///     The <see cref="OutboundProducerBehavior" /> sort index.
        /// </summary>
        public const int OutboundProducer = 200;

        /// <summary>
        ///     The <see cref="OutboundRouterBehavior" /> sort index.
        /// </summary>
        public const int OutboundRouter = 300;
    }
}
