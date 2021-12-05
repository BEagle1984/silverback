// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The messages are stored in a the transactional outbox table. The operation is therefore included in the database transaction
///     applying the message side effects to the local database. The <see cref="IOutboxWorker" /> takes care of asynchronously sending
///     the messages to the message broker.
/// </summary>
public sealed class OutboxProduceStrategy : IProduceStrategy, IEquatable<OutboxProduceStrategy>
{
    /// <inheritdoc cref="op_Equality" />
    public static bool operator ==(OutboxProduceStrategy? left, OutboxProduceStrategy? right) => Equals(left, right);

    /// <inheritdoc cref="op_Inequality" />
    public static bool operator !=(OutboxProduceStrategy? left, OutboxProduceStrategy? right) => !Equals(left, right);

    /// <inheritdoc cref="IProduceStrategy.Build" />
    public IProduceStrategyImplementation Build(IServiceProvider serviceProvider) =>
        new OutboxProduceStrategyImplementation(
            serviceProvider.GetRequiredService<TransactionalOutboxBroker>(),
            serviceProvider.GetRequiredService<IOutboundLogger<OutboxProduceStrategy>>());

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(OutboxProduceStrategy? other) => other != null;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(IProduceStrategy? other) => other is OutboxProduceStrategy;

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => obj is OutboxProduceStrategy;

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => GetType().GetHashCode();

    private sealed class OutboxProduceStrategyImplementation : IProduceStrategyImplementation
    {
        private readonly TransactionalOutboxBroker _outboundQueueBroker;

        private readonly IOutboundLogger<OutboxProduceStrategy> _logger;

        private readonly SemaphoreSlim _producerInitSemaphore = new(1, 1);

        private IProducer? _producer;

        public OutboxProduceStrategyImplementation(
            TransactionalOutboxBroker outboundQueueBroker,
            IOutboundLogger<OutboxProduceStrategy> logger)
        {
            _outboundQueueBroker = outboundQueueBroker;
            _logger = logger;
        }

        public async Task ProduceAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            _logger.LogWrittenToOutbox(envelope);

            _producer ??= await InitProducerAsync(envelope.Endpoint.Configuration).ConfigureAwait(false);

            await _producer.ProduceAsync(envelope).ConfigureAwait(false);
        }

        private async Task<IProducer> InitProducerAsync(ProducerConfiguration configuration)
        {
            await _producerInitSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _producer = await _outboundQueueBroker.GetProducerAsync(configuration).ConfigureAwait(false);
                return _producer;
            }
            finally
            {
                _producerInitSemaphore.Release();
            }
        }
    }
}
