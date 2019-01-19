// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
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
        public static BrokerOptionsBuilder AddDbInboundConnector<TDbContext>(this BrokerOptionsBuilder builder)
            where TDbContext : DbContext
        {
            builder.AddInboundConnector<LoggedInboundConnector>();
            builder.Services.AddScoped<IInboundLog>(s =>
                new DbContextInboundLog(s.GetRequiredService<TDbContext>(),
                    s.GetRequiredService<MessageKeyProvider>()));

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
            builder.AddOutboundConnector<DeferredOutboundConnector>();
            builder.Services.AddScoped<IOutboundQueueProducer>(s =>
                new DbContextOutboundQueueProducer(s.GetRequiredService<TDbContext>(),
                    s.GetRequiredService<MessageKeyProvider>()));

            return builder;
        }

        /// <summary>
        /// Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="enforceMessageOrder">if set to <c>true</c> the message order will be preserved (no message will be skipped).</param>
        /// <param name="readPackageSize">The number of messages to be loaded from the queue at once.</param>
        /// <param name="removeProduced">if set to <c>true</c> the messages will be removed from the database immediately after being produced.</param>
        public static BrokerOptionsBuilder AddDbOutboundWorker<TDbContext>(this BrokerOptionsBuilder builder,
            bool enforceMessageOrder = true, int readPackageSize = 100, bool removeProduced = false)
            where TDbContext : DbContext
        {
            builder.AddOutboundWorker(enforceMessageOrder, readPackageSize);
            builder.Services.AddScoped<IOutboundQueueConsumer>(s =>
                new DbContextOutboundQueueConsumer(s.GetRequiredService<TDbContext>(),
                    s.GetRequiredService<MessageKeyProvider>(), removeProduced));

            return builder;
        }
    }
}
