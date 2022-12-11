// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaProducerConfiguration" />.
/// </summary>
public partial class KafkaProducerConfigurationBuilder : KafkaClientConfigurationBuilder<ProducerConfig, KafkaProducerConfigurationBuilder>
{
    private readonly Dictionary<string, KafkaProducerEndpointConfiguration> _endpoints = new();

    private bool? _throwIfNotAcknowledged;

    private bool? _disposeOnException;

    private TimeSpan? _flushTimeout;

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}.This" />
    protected override KafkaProducerConfigurationBuilder This => this;

    // TODO: Produce without type name? To create producer config without actual outbound route (mapped to no type)

    /// <summary>
    ///     Adds a producer endpoint, which is a topic or partition and its related configuration (serializer, etc.).
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder Produce(Action<KafkaProducerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Produce<object>(configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic or partition and its related configuration (serializer, etc.).
    /// </summary>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name and the message type.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder Produce(string? name, Action<KafkaProducerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Produce<object>(name, configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic or partition and its related configuration (serializer, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being produced. This is used to setup the serializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder Produce<TMessage>(Action<KafkaProducerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction) =>
        Produce(null, configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic or partition and its related configuration (serializer, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being produced. This is used to setup the serializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name and the message type.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaProducerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder Produce<TMessage>(
        string? name,
        Action<KafkaProducerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaProducerEndpointConfigurationBuilder<TMessage> builder = new(name);
        configurationBuilderAction.Invoke(builder);
        KafkaProducerEndpointConfiguration endpointConfiguration = builder.Build();

        name ??= $"{endpointConfiguration.RawName}|{typeof(TMessage).FullName}"; // TODO: Check if OK to concat type (needed when pushing multiple types to same topic)

        if (_endpoints.ContainsKey(name))
            return this;

        _endpoints[name] = endpointConfiguration;

        return this;
    }

    /// <summary>
    ///     Specifies that an exception must be thrown by the producer if the persistence is not acknowledge by the broker.
    ///     This is the default.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder ThrowIfNotAcknowledged()
    {
        _throwIfNotAcknowledged = true;
        return this;
    }

    /// <summary>
    ///     Specifies that no exception has be thrown by the producer if the persistence is not acknowledge by the broker.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder IgnoreIfNotAcknowledged()
    {
        _throwIfNotAcknowledged = false;
        return this;
    }

    /// <summary>
    ///     Specifies that the producer has to be disposed and recreated if a <see cref="KafkaException" /> is thrown.
    ///     This is the default.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisposeOnException()
    {
        _disposeOnException = true;
        return this;
    }

    /// <summary>
    ///     Specifies tjat the producer don't have to be disposed and recreated if a <see cref="KafkaException" /> is thrown.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableDisposeOnException()
    {
        _disposeOnException = false;
        return this;
    }

    /// <summary>
    ///     Specifies the flush operation timeout. The default is 30 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The flush operation timeout.
    /// </param>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithFlushTimeout(TimeSpan timeout)
    {
        _flushTimeout = timeout;
        return this;
    }

    /// <summary>
    ///     Enable notification of delivery reports.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableDeliveryReports() => WithEnableDeliveryReports(true);

    /// <summary>
    ///     Disable notification of delivery reports. This will enable "fire and forget" semantics and give a small boost in performance.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableDeliveryReports() => WithEnableDeliveryReports(false);

    /// <summary>
    ///     The producer will ensure that messages are successfully produced exactly once and in the original produce order.
    ///     The following configuration properties are adjusted automatically (if not modified by the user) when idempotence is enabled:
    ///     `max.in.flight.requests.per.connection=5` (must be less than or equal to 5), `retries=INT32_MAX` (must be greater than 0),
    ///     `acks=all`, `queuing.strategy=fifo`. Producer instantiation will fail if user-supplied configuration is incompatible.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableIdempotence() => WithEnableIdempotence(true);

    /// <summary>
    ///     The producer will <b>not</b> ensure that messages are successfully produced exactly once and in the original produce order.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableIdempotence() => WithEnableIdempotence(false);

    /// <summary>
    ///     Builds the <see cref="KafkaProducerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfiguration" />.
    /// </returns>
    public KafkaProducerConfiguration Build()
    {
        KafkaProducerConfiguration configuration = new(ClientConfig);

        configuration = configuration with
        {
            ThrowIfNotAcknowledged = _throwIfNotAcknowledged ?? configuration.ThrowIfNotAcknowledged,
            DisposeOnException = _disposeOnException ?? configuration.DisposeOnException,
            FlushTimeout = _flushTimeout ?? configuration.FlushTimeout,
            Endpoints = _endpoints.Values.AsValueReadOnlyCollection()
        };

        configuration.Validate();

        return configuration;
    }
}
