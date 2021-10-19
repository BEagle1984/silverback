// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <summary>
///     An <see cref="IBroker" /> implementation for Apache Kafka.
/// </summary>
public class KafkaBroker : Broker<KafkaProducerConfiguration, KafkaConsumerConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaBroker" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public KafkaBroker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    ///     Returns an <see cref="KafkaProducer" /> to be used to produce to the specified endpoint.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducer" /> for the specified endpoint.
    /// </returns>
    public KafkaProducer GetProducer(Action<KafkaProducerConfigurationBuilder<object>> configurationBuilderAction) =>
        GetProducer<object>(configurationBuilderAction);

    /// <summary>
    ///     Returns an <see cref="KafkaProducer" /> to be used to produce to the specified endpoint.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages that will be produced to this endpoint.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducer" /> for the specified endpoint.
    /// </returns>
    public KafkaProducer GetProducer<TMessage>(Action<KafkaProducerConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaProducerConfigurationBuilder<TMessage> builder = new();
        configurationBuilderAction.Invoke(builder);
        return (KafkaProducer)GetProducer(builder.Build());
    }

    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
    protected override IProducer InstantiateProducer(
        KafkaProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new KafkaProducer(
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider.GetRequiredService<IConfluentProducersCache>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IOutboundLogger<KafkaProducer>>());

    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
    protected override IConsumer InstantiateConsumer(
        KafkaConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new KafkaConsumer(
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IConfluentConsumerBuilder>(),
            serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IInboundLogger<KafkaConsumer>>());
}
