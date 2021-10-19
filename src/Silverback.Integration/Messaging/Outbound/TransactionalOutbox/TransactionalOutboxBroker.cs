// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     An <see cref="IBroker" /> implementation that is used by the <see cref="OutboxProduceStrategy" /> to
///     write into the outbox.
/// </summary>
public class TransactionalOutboxBroker : Broker<ProducerConfiguration, ConsumerConfiguration>
{
    private readonly IOutboxWriter _queueWriter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransactionalOutboxBroker" /> class.
    /// </summary>
    /// <param name="queueWriter">
    ///     The <see cref="IOutboxWriter" /> to be used to write to the queue.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public TransactionalOutboxBroker(
        IOutboxWriter queueWriter,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _queueWriter = queueWriter;
    }

    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
    protected override IProducer InstantiateProducer(
        ProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new OutboundQueueProducer(
            _queueWriter,
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IOutboundLogger<OutboundQueueProducer>>());

    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
    protected override IConsumer InstantiateConsumer(
        ConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        throw new InvalidOperationException(
            "This IBroker implementation is used to write to outbound queue. " +
            "Only the producers are therefore supported.");
}
