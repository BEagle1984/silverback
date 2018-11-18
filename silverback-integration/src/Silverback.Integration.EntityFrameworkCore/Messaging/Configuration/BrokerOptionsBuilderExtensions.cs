using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Connectors.Repositories;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test?
    public static class BrokerOptionsBuilderExtensions
    {
        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation logs the incoming messages in the DbContext and prevents duplicated processing of the same message.
        /// </summary>
        public static BrokerOptionsBuilder AddDbContextInboundConnector(this BrokerOptionsBuilder builder) =>
            builder.AddLoggedInboundConnector<DbContextInboundLog>();

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// This implementation stores the outbound messages into an intermediate queue in the DbContext and
        /// it is therefore fully transactional.
        /// </summary>
        public static BrokerOptionsBuilder AddDbContextOutboundConnector(this BrokerOptionsBuilder builder) =>
            builder.AddDeferredOutboundConnector<DbContextOutboundQueueProducer>();
    }
}
