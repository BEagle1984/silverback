// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     An <see cref="IBroker" /> implementation that is used by the <see cref="OutboxProduceStrategy" /> to
///     write into the outbox.
/// </summary>
public class OutboxBroker : Broker<ProducerConfiguration, ConsumerConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxBroker" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public OutboxBroker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <inheritdoc cref="Broker{TProducerConfiguration,TConsumerConfiguration}.InstantiateProducer" />
    protected override IProducer InstantiateProducer(
        ProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider)
    {
        OutboxProduceStrategy outboxStrategy = (OutboxProduceStrategy)Check.NotNull(configuration, nameof(configuration)).Strategy;

        return new OutboxProducer(
            serviceProvider.GetRequiredService<IOutboxWriterFactory>().GetWriter(outboxStrategy.Settings),
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IOutboundLogger<OutboxProducer>>());
    }

    /// <inheritdoc cref="Broker{TProducerConfiguration,TConsumerConfiguration}.InstantiateConsumer" />
    protected override IConsumer InstantiateConsumer(
        ConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        throw new InvalidOperationException(
            "This IBroker implementation is used to write to the outbox. " +
            "Only the producers are therefore supported.");
}
