// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <summary>
///     An <see cref="IBroker" /> implementation for MQTT.
/// </summary>
public class MqttBroker : Broker<MqttProducerConfiguration, MqttConsumerConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttBroker" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public MqttBroker(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    ///     Returns an <see cref="MqttProducer" /> to be used to produce to the specified endpoint.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducer" /> for the specified endpoint.
    /// </returns>
    public MqttProducer GetProducer(Action<MqttProducerConfigurationBuilder<object>> configurationBuilderAction) =>
        GetProducer<object>(configurationBuilderAction);

    /// <summary>
    ///     Returns an <see cref="MqttProducer" /> to be used to produce to the specified endpoint.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages that will be produced to this endpoint.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttProducer" /> for the specified endpoint.
    /// </returns>
    public MqttProducer GetProducer<TMessage>(Action<MqttProducerConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttProducerConfigurationBuilder<TMessage> builder = new();
        configurationBuilderAction.Invoke(builder);
        return (MqttProducer)GetProducer(builder.Build());
    }

    /// <inheritdoc cref="Broker{TProducerConfiguration,TConsumerConfiguration}.InstantiateProducer" />
    protected override IProducer InstantiateProducer(
        MqttProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new MqttProducer(
            this,
            configuration,
            behaviorsProvider,
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IOutboundLogger<MqttProducer>>());

    /// <inheritdoc cref="Broker{TProducerConfiguration,TConsumerConfiguration}.InstantiateConsumer" />
    protected override IConsumer InstantiateConsumer(
        MqttConsumerConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider) =>
        new MqttConsumer(
            this,
            configuration,
            behaviorsProvider,
            serviceProvider,
            serviceProvider.GetRequiredService<IInboundLogger<MqttConsumer>>());
}
