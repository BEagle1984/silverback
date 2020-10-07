// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Deferred;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
    /// <summary>
    ///     The messages are stored in a the transactional outbox table. The operation is therefore included in
    ///     database
    ///     transaction used to update the data. The <see cref="IOutboxWorker" /> takes care of asynchronously
    ///     sending the messages to the message broker.
    /// </summary>
    public class TransactionalOutboxProduceStrategy : IProduceStrategy
    {
        /// <inheritdoc cref="IProduceStrategy.Build" />
        public IProduceStrategyImplementation Build(IServiceProvider serviceProvider) =>
            new TransactionalOutboxProduceStrategyImplementation(
                serviceProvider.GetRequiredService<TransactionalOutboxBroker>(),
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<TransactionalOutboxProduceStrategy>>());

        private class TransactionalOutboxProduceStrategyImplementation : IProduceStrategyImplementation
        {
            private readonly TransactionalOutboxBroker _outboundQueueBroker;

            private readonly ISilverbackIntegrationLogger<TransactionalOutboxProduceStrategy> _logger;

            public TransactionalOutboxProduceStrategyImplementation(
                TransactionalOutboxBroker outboundQueueBroker,
                ISilverbackIntegrationLogger<TransactionalOutboxProduceStrategy> logger)
            {
                _outboundQueueBroker = outboundQueueBroker;
                _logger = logger;
            }

            public Task ProduceAsync(IOutboundEnvelope envelope)
            {
                Check.NotNull(envelope, nameof(envelope));

                _logger.LogDebugWithMessageInfo(
                    IntegrationEventIds.OutboundMessageWrittenToOutbox,
                    "Writing the outbound message to the transactional outbox.",
                    envelope);

                return _outboundQueueBroker.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
            }
        }
    }
}
