// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Configures the Kafka producers and consumers.
/// </summary>
public sealed class KafkaEndpointsConfigurationBuilder : EndpointsConfigurationBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaEndpointsConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public KafkaEndpointsConfigurationBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    internal KafkaClientConfiguration ClientConfiguration { get; } = new();

    /// <summary>
    ///     Configures the Kafka client properties that are shared between the producers and consumers.
    /// </summary>
    /// <param name="configAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaClientConfiguration" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaEndpointsConfigurationBuilder ConfigureClient(Action<KafkaClientConfiguration> configAction)
    {
        Check.NotNull(configAction, nameof(configAction));

        configAction.Invoke(ClientConfiguration);

        return this;
    }

    /// <summary>
    ///     Configures an outbound endpoint with the specified producer configuration.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced to this endpoint.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="preloadProducers">
    ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
    ///     <see cref="KafkaProducer" /> will be created only when the first message is about to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaEndpointsConfigurationBuilder AddOutbound<TMessage>(
        Action<KafkaProducerConfigurationBuilder<TMessage>> configurationBuilderAction,
        bool preloadProducers = true)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaProducerConfiguration? endpointConfiguration = BuildAndValidateConfiguration(configurationBuilderAction);

        if (endpointConfiguration != null)
            AddOutbound<TMessage>(endpointConfiguration, preloadProducers);

        return this;
    }

    /// <summary>
    ///     Configures an inbound endpoint with the specified consumer configuration.
    /// </summary>
    /// <remarks>
    ///     Multiple calls to this methods will cause multiple consumers to be instantiated, which means multiple connections being issues
    ///     and more resources being used. The <see cref="KafkaConsumerEndpoint" /> allows to define multiple topics to be consumed, to
    ///     efficiently instantiate a single consumer for all of them.
    /// </remarks>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="consumersCount">
    ///     The number of consumers to be instantiated. The default is 1.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaEndpointsConfigurationBuilder AddInbound(
        Action<KafkaConsumerConfigurationBuilder<object>> configurationBuilderAction,
        int consumersCount = 1) =>
        AddInbound<object>(configurationBuilderAction, consumersCount);

    /// <summary>
    ///     Configures an inbound endpoint with the specified consumer configuration.
    /// </summary>
    /// <remarks>
    ///     Multiple calls to this methods will cause multiple consumers to be instantiated, which means multiple connections being issues
    ///     and more resources being used. The <see cref="KafkaConsumerEndpoint" /> allows to define multiple topics to be consumed, to
    ///     efficiently instantiate a single consumer for all of them.
    /// </remarks>
    /// <typeparam name="TMessage">
    ///     The type of the messages that will be consumed from this endpoint. Specifying the message type will usually automatically switch to the typed message serializer and deserialize this specific type,
    ///     regardless of the message headers.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="consumersCount">
    ///     The number of consumers to be instantiated. The default is 1.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaEndpointsConfigurationBuilder AddInbound<TMessage>(
        Action<KafkaConsumerConfigurationBuilder<TMessage>> configurationBuilderAction,
        int consumersCount = 1)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaConsumerConfiguration? endpointConfiguration = BuildAndValidateConfiguration(configurationBuilderAction);

        if (endpointConfiguration != null)
            AddInbound(endpointConfiguration, consumersCount);

        return this;
    }

    private KafkaProducerConfiguration? BuildAndValidateConfiguration<TMessage>(Action<KafkaProducerConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        KafkaProducerConfigurationBuilder<TMessage> builder = new(ClientConfiguration, this);
        KafkaProducerConfiguration? endpointConfiguration = builder.BuildAndValidate(
            configurationBuilderAction,
            ServiceProvider.GetService<ISilverbackLogger<KafkaEndpointsConfigurationBuilder>>());
        return endpointConfiguration;
    }

    private KafkaConsumerConfiguration? BuildAndValidateConfiguration<TMessage>(Action<KafkaConsumerConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        KafkaConsumerConfigurationBuilder<TMessage> builder = new(ClientConfiguration, this);
        KafkaConsumerConfiguration? endpointConfiguration = builder.BuildAndValidate(
            configurationBuilderAction,
            ServiceProvider.GetService<ISilverbackLogger<KafkaEndpointsConfigurationBuilder>>());
        return endpointConfiguration;
    }
}
