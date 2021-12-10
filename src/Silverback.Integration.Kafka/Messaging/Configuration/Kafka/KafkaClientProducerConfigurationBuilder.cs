// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaClientProducerConfiguration" />.
/// </summary>
public partial class KafkaClientProducerConfigurationBuilder
    : KafkaClientConfigurationBuilder<ProducerConfig, KafkaClientProducerConfigurationBuilder>
{
    private bool? _throwIfNotAcknowledged;

    private bool? _disposeOnException;

    private TimeSpan? _flushTimeout;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientProducerConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientProducerConfiguration" /> to be used to initialize the <see cref="KafkaClientProducerConfiguration" />.
    /// </param>
    public KafkaClientProducerConfigurationBuilder(KafkaClientProducerConfiguration clientConfig)
        : base(clientConfig)
    {
        // TODO: test to ensure we don't forget any assignment
        _throwIfNotAcknowledged = clientConfig?.ThrowIfNotAcknowledged;
        _disposeOnException = clientConfig?.DisposeOnException;
        _flushTimeout = clientConfig?.FlushTimeout;

        ProducerConfig = new ProducerConfig(ClientConfig);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientProducerConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientProducerConfiguration" />.
    /// </param>
    public KafkaClientProducerConfigurationBuilder(KafkaClientConfiguration? clientConfig = null)
        : base(clientConfig)
    {
        ProducerConfig = new ProducerConfig(ClientConfig);
    }

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}.This" />
    protected override KafkaClientProducerConfigurationBuilder This => this;

    private ProducerConfig ProducerConfig { get; }

    /// <summary>
    ///     Specifies that an exception must be thrown by the producer if the persistence is not acknowledge by the broker.
    ///     This is the default.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientProducerConfigurationBuilder ThrowIfNotAcknowledged()
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
    public KafkaClientProducerConfigurationBuilder IgnoreIfNotAcknowledged()
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
    public KafkaClientProducerConfigurationBuilder DisposeOnException()
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
    public KafkaClientProducerConfigurationBuilder DisableDisposeOnException()
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
    public KafkaClientProducerConfigurationBuilder WithFlushTimeout(TimeSpan timeout)
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
    public KafkaClientProducerConfigurationBuilder EnableDeliveryReports() => WithEnableDeliveryReports(true);

    /// <summary>
    ///     Disable notification of delivery reports. This will enable "fire and forget" semantics and give a small boost in performance.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientProducerConfigurationBuilder DisableDeliveryReports() => WithEnableDeliveryReports(false);

    /// <summary>
    ///     The producer will ensure that messages are successfully produced exactly once and in the original produce order.
    ///     The following configuration properties are adjusted automatically (if not modified by the user) when idempotence is enabled:
    ///     `max.in.flight.requests.per.connection=5` (must be less than or equal to 5), `retries=INT32_MAX` (must be greater than 0),
    ///     `acks=all`, `queuing.strategy=fifo`. Producer instantiation will fail if user-supplied configuration is incompatible.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientProducerConfigurationBuilder EnableIdempotence() => WithEnableIdempotence(true);

    /// <summary>
    ///     The producer will <b>not</b> ensure that messages are successfully produced exactly once and in the original produce order.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientProducerConfigurationBuilder DisableIdempotence() => WithEnableIdempotence(false);

    /// <summary>
    ///     Builds the <see cref="KafkaClientProducerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientProducerConfiguration" />.
    /// </returns>
    public KafkaClientProducerConfiguration Build()
    {
        KafkaClientProducerConfiguration config = new(ProducerConfig);

        return config with
        {
            ThrowIfNotAcknowledged = _throwIfNotAcknowledged ?? config.ThrowIfNotAcknowledged,
            DisposeOnException = _disposeOnException ?? config.DisposeOnException,
            FlushTimeout = _flushTimeout ?? config.FlushTimeout
        };
    }
}
