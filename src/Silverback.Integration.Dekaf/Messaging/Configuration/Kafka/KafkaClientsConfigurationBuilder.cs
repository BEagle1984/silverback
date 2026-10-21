// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Configures the Kafka producers and consumers building the <see cref="KafkaProducerConfiguration" /> and
///     <see cref="KafkaConsumerConfiguration" />.
/// </summary>
public class KafkaClientsConfigurationBuilder
{
    private readonly KafkaClientsConfigurationActions _configurationActions = new();

    private readonly List<Action<KafkaProducerConfigurationBuilder>> _defaultProducerConfigurationActions = [];

    private readonly List<Action<KafkaConsumerConfigurationBuilder>> _defaultConsumerConfigurationActions = [];

    private readonly List<Action<IKafkaClientConfigurationBuilder>> _sharedConfigurationActions = [];

    /// <summary>
    ///     Adds a Kafka producer.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction) =>
        AddProducer(Guid.NewGuid().ToString(), configurationBuilderAction);

    /// <summary>
    ///     Adds a Kafka producer or updates its configuration if a producer with the same name already exists.
    /// </summary>
    /// <param name="name">
    ///     The producer name.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddProducer(string name, Action<KafkaProducerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _configurationActions.ProducerConfigurationActions.AddOrAppend(name, configurationBuilderAction);

        return this;
    }

    /// <summary>
    ///     Adds a Kafka consumer.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddConsumer(Action<KafkaConsumerConfigurationBuilder> configurationBuilderAction) =>
        AddConsumer(Guid.NewGuid().ToString(), configurationBuilderAction);

    /// <summary>
    ///     Adds a Kafka consumer or updates its configuration if a consumer with the same name already exists.
    /// </summary>
    /// <param name="name">
    ///     The consumer name.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddConsumer(string name, Action<KafkaConsumerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _configurationActions.ConsumerConfigurationActions.AddOrAppend(name, configurationBuilderAction);

        return this;
    }

    /// <summary>
    ///     Add a default producer configuration that will be applied to all the producers.<br />
    ///     This configuration action will be applied before any other producer-specific configuration action.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddDefaultProducerConfiguration(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _defaultProducerConfigurationActions.Add(configurationBuilderAction);

        return this;
    }

    /// <summary>
    ///     Add a default consumer configuration that will be applied to all the consumers.<br />
    ///     This configuration action will be applied before any other consumer-specific configuration action.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder AddDefaultConsumerConfiguration(Action<KafkaConsumerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _defaultConsumerConfigurationActions.Add(configurationBuilderAction);

        return this;
    }

    /// <summary>
    ///     Sets the comma-separated list of brokers (host or host:port) for all clients.
    /// </summary>
    /// <param name="bootstrapServers">
    ///     The comma-separated list of brokers.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder WithBootstrapServers(string? bootstrapServers)
    {
        _sharedConfigurationActions.Add(builder => builder.WithBootstrapServers(bootstrapServers));
        return this;
    }

    /// <summary>
    ///     Sets the client identifier for all clients.
    /// </summary>
    /// <param name="clientId">
    ///     The client identifier.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaClientsConfigurationBuilder WithClientId(string? clientId)
    {
        _sharedConfigurationActions.Add(builder => builder.WithClientId(clientId));
        return this;
    }

    internal KafkaClientsConfigurationActions GetConfigurationActions()
    {
        foreach (Action<KafkaProducerConfigurationBuilder> defaultAction in _defaultProducerConfigurationActions)
        {
            _configurationActions.ProducerConfigurationActions.PrependToAll(defaultAction.Invoke);
        }

        foreach (Action<KafkaConsumerConfigurationBuilder> defaultAction in _defaultConsumerConfigurationActions)
        {
            _configurationActions.ConsumerConfigurationActions.PrependToAll(defaultAction.Invoke);
        }

        foreach (Action<IKafkaClientConfigurationBuilder> sharedAction in _sharedConfigurationActions)
        {
            _configurationActions.ProducerConfigurationActions.PrependToAll(sharedAction.Invoke);
            _configurationActions.ConsumerConfigurationActions.PrependToAll(sharedAction.Invoke);
        }

        return _configurationActions;
    }
}
