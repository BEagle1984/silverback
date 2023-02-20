// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
    /// <summary>
    ///     The messages are stored in a the transactional outbox table. The operation is therefore included in
    ///     the database transaction applying the message side effects to the local database. The
    ///     <see cref="IOutboxWorker" /> takes care of asynchronously sending the messages to the message broker.
    /// </summary>
    public class OutboxProduceStrategy : IProduceStrategy
    {
        /// <inheritdoc cref="IProduceStrategy.Build" />
        public IProduceStrategyImplementation Build(IServiceProvider serviceProvider) =>
            new OutboxProduceStrategyImplementation(
                serviceProvider.GetRequiredService<TransactionalOutboxBroker>(),
                serviceProvider.GetRequiredService<IOutboundLogger<OutboxProduceStrategy>>());

        private sealed class OutboxProduceStrategyImplementation : IProduceStrategyImplementation
        {
            private readonly TransactionalOutboxBroker _outboundQueueBroker;

            private readonly IOutboundLogger<OutboxProduceStrategy> _logger;

            private IProducer? _producer;

            public OutboxProduceStrategyImplementation(
                TransactionalOutboxBroker outboundQueueBroker,
                IOutboundLogger<OutboxProduceStrategy> logger)
            {
                _outboundQueueBroker = outboundQueueBroker;
                _logger = logger;
            }

            public Task ProduceAsync(IOutboundEnvelope envelope)
            {
                Check.NotNull(envelope, nameof(envelope));

                _logger.LogWrittenToOutbox(envelope);

                if (_producer == null || _producer.Endpoint.Name != envelope.Endpoint.Name)
                {
                    _producer = _outboundQueueBroker.GetProducer(envelope.Endpoint);
                }

                return _producer.ProduceAsync(envelope);
            }
        }
    }
}
