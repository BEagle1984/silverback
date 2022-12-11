// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Configures the Kafka producers and consumers building the <see cref="KafkaProducerConfiguration" /> and
///     <see cref="KafkaConsumerConfiguration" />.
/// </summary>
/// TODO: Test idempotency (e.g. AddOrMergeProducer should distinct on message type and topic)
public partial class KafkaClientsConfigurationBuilder
{
    private readonly KafkaClientsConfigurationActions _configurationActions = new();

    private readonly List<Action<IKafkaClientConfigurationBuilder>> _sharedConfigurationActions = new();

    // TODO: Autogenerate this and other methods
    // public KafkaClientsConfigurationBuilder WithBootstrapServers(string? bootstrapServers)
    // {
    //     _sharedConfigurationActions.Add(builder => builder.WithBootstrapServers(bootstrapServers));
    //     return this;
    // }

    /// <summary>
    ///     Add as a Kafka producer.
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
    ///     Add as a Kafka consumer.
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

    internal KafkaClientsConfigurationActions GetConfigurationActions()
    {
        foreach (Action<IKafkaClientConfigurationBuilder> sharedAction in _sharedConfigurationActions)
        {
            _configurationActions.ProducerConfigurationActions.PrependToAll(builder => sharedAction.Invoke(builder));
            _configurationActions.ConsumerConfigurationActions.PrependToAll(builder => sharedAction.Invoke(builder));
        }

        return _configurationActions;
    }
}
