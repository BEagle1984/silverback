// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test?
    public static class BrokerOptionsBuilderExtensions
    {
        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation logs the incoming messages in the DbContext and prevents duplicated processing of the same message.
        /// </summary>
        public static BrokerOptionsBuilder AddDbLoggedInboundConnector<TDbContext>(this BrokerOptionsBuilder builder)
            where TDbContext : DbContext
        {
            builder.AddLoggedInboundConnector(s =>
                new DbContextInboundLog(s.GetRequiredService<TDbContext>(),
                    s.GetRequiredService<MessageKeyProvider>()));

            return builder;
        }

        /// <summary>
        /// Adds a connector to subscribe to a message broker and forward the incoming integration messages to the internal bus.
        /// This implementation stores the offset of the latest consumed messages in the DbContext and prevents duplicated processing of the same message.
        /// </summary>
        public static BrokerOptionsBuilder AddDbOffsetStoredInboundConnector<TDbContext>(this BrokerOptionsBuilder builder)
            where TDbContext : DbContext
        {
            builder.AddOffsetStoredInboundConnector(s =>
                new DbContextOffsetStore(s.GetRequiredService<TDbContext>()));

            return builder;
        }

        /// <summary>
        /// Adds a connector to publish the integration messages to the configured message broker.
        /// This implementation stores the outbound messages into an intermediate queue in the DbContext and
        /// it is therefore fully transactional.
        /// </summary>
        public static BrokerOptionsBuilder AddDbOutboundConnector<TDbContext>(this BrokerOptionsBuilder builder)
            where TDbContext : DbContext
        {
            builder.AddDeferredOutboundConnector(s =>
                new DbContextOutboundQueueProducer(s.GetRequiredService<TDbContext>()));

            return builder;
        }

        /// <summary>
        /// Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="enforceMessageOrder">if set to <c>true</c> the message order will be preserved (no message will be skipped).</param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="removeProduced">if set to <c>true</c> the messages will be removed from the database immediately after being produced.</param>
        public static BrokerOptionsBuilder AddDbOutboundWorker<TDbContext>(this BrokerOptionsBuilder builder,
            bool enforceMessageOrder = true, int readPackageSize = 100, bool removeProduced = true)
            where TDbContext : DbContext
        {
            builder.AddOutboundWorker(
                s => new DbContextOutboundQueueConsumer(s.GetRequiredService<TDbContext>(), removeProduced),
                enforceMessageOrder, readPackageSize);

            return builder;
        }

        /// <summary>
        /// Adds a chunk store to temporary save the message chunks until the full message has been received.
        /// This implementation stores the message chunks in the DbContext.
        /// </summary>
        public static BrokerOptionsBuilder AddDbChunkStore<TDbContext>(this BrokerOptionsBuilder builder)
            where TDbContext : DbContext
        {
            builder.AddChunkStore(s => new DbContextChunkStore(
                s.GetRequiredService<TDbContext>(),
                s.GetRequiredService<ILogger<DbContextChunkStore>>()));

            return builder;
        }
    }
}
