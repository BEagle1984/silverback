// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Configures the MQTT producers and consumers.
/// </summary>
public sealed class MqttEndpointsConfigurationBuilder : EndpointsConfigurationBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttEndpointsConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public MqttEndpointsConfigurationBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    internal MqttClientConfiguration ClientConfiguration { get; private set; } = new();

    /// <summary>
    ///     Configures the MQTT client properties that are shared between the producers and consumers.
    /// </summary>
    /// <param name="configAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfiguration" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttEndpointsConfigurationBuilder ConfigureClient(Action<MqttClientConfiguration> configAction)
    {
        Check.NotNull(configAction, nameof(configAction));

        configAction.Invoke(ClientConfiguration);

        return this;
    }

    /// <summary>
    ///     Configures the MQTT client properties that are shared between the producers and consumers.
    /// </summary>
    /// <param name="configBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttEndpointsConfigurationBuilder ConfigureClient(Action<MqttClientConfigurationBuilder> configBuilderAction)
    {
        Check.NotNull(configBuilderAction, nameof(configBuilderAction));

        MqttClientConfigurationBuilder configurationBuilder = new(ServiceProvider);
        configBuilderAction.Invoke(configurationBuilder);

        ClientConfiguration = configurationBuilder.Build();

        return this;
    }

    /// <summary>
    ///     Configures an outbound endpoint with the specified producer configuration.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced to this endpoint.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="preloadProducers">
    ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
    ///     <see cref="MqttProducer" /> will be created only when the first message is about to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttEndpointsConfigurationBuilder AddOutbound<TMessage>(
        Action<MqttProducerConfigurationBuilder<TMessage>> configurationBuilderAction,
        bool preloadProducers = true)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttProducerConfiguration? endpointConfiguration = BuildAndValidateConfiguration(configurationBuilderAction);

        if (endpointConfiguration != null)
            AddOutbound<TMessage>(endpointConfiguration, preloadProducers);

        return this;
    }

    /// <summary>
    ///     Configures an inbound endpoint with the specified consumer configuration.
    /// </summary>
    /// <remarks>
    ///     Multiple calls to this methods will cause multiple consumers to be instantiated, which means multiple connections being issues
    ///     and more resources being used. The <see cref="MqttConsumerEndpoint" /> allows to define multiple topics to be consumed, to
    ///     efficiently instantiate a single consumer for all of them.
    /// </remarks>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="consumersCount">
    ///     The number of consumers to be instantiated. The default is 1.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttEndpointsConfigurationBuilder AddInbound(
        Action<MqttConsumerConfigurationBuilder<object>> configurationBuilderAction,
        int consumersCount = 1) =>
        AddInbound<object>(configurationBuilderAction, consumersCount);

    /// <summary>
    ///     Configures an inbound endpoint with the specified consumer configuration.
    /// </summary>
    /// <remarks>
    ///     Multiple calls to this methods will cause multiple consumers to be instantiated, which means multiple connections being issues
    ///     and more resources being used. The <see cref="MqttConsumerEndpoint" /> allows to define multiple topics to be consumed, to
    ///     efficiently instantiate a single consumer for all of them.
    /// </remarks>
    /// <typeparam name="TMessage">
    ///     The type of the messages that will be consumed from this endpoint. Specifying the message type will usually automatically switch to the typed message serializer and deserialize this specific type,
    ///     regardless of the message headers.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttConsumerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="consumersCount">
    ///     The number of consumers to be instantiated. The default is 1.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttEndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttEndpointsConfigurationBuilder AddInbound<TMessage>(
        Action<MqttConsumerConfigurationBuilder<TMessage>> configurationBuilderAction,
        int consumersCount = 1)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttConsumerConfiguration? endpointConfiguration = BuildAndValidateConfiguration(configurationBuilderAction);

        if (endpointConfiguration != null)
            AddInbound(endpointConfiguration, consumersCount);

        return this;
    }

    private MqttProducerConfiguration? BuildAndValidateConfiguration<TMessage>(Action<MqttProducerConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        MqttProducerConfigurationBuilder<TMessage> builder = new(ClientConfiguration, this);
        MqttProducerConfiguration? endpointConfiguration = builder.BuildAndValidate(
            configurationBuilderAction,
            ServiceProvider.GetService<ISilverbackLogger<MqttEndpointsConfigurationBuilder>>());
        return endpointConfiguration;
    }

    private MqttConsumerConfiguration? BuildAndValidateConfiguration<TMessage>(Action<MqttConsumerConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        MqttConsumerConfigurationBuilder<TMessage> builder = new(ClientConfiguration, this);
        MqttConsumerConfiguration? endpointConfiguration = builder.BuildAndValidate(
            configurationBuilderAction,
            ServiceProvider.GetService<ISilverbackLogger<MqttEndpointsConfigurationBuilder>>());
        return endpointConfiguration;
    }
}
