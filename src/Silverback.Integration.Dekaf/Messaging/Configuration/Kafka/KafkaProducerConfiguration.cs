// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The <see cref="KafkaProducer" /> configuration.
/// </summary>
/// <remarks>
///     When adding a client setting:
///     1. Add the property here
///     2. Add the field, the method and the <c>Build</c> mapping in <see cref="KafkaProducerConfigurationBuilder" />
///     3. Map it in <c>DekafClientsFactory</c>
///     4. List it in the Dekaf documentation page (docs/guides/broker/dekaf.md).
///     The settings keep the names of the Confluent.Kafka based integration (e.g. <see cref="LingerMs" />), so that
///     the configuration code can be shared.
/// </remarks>
public sealed record KafkaProducerConfiguration : KafkaClientConfiguration
{
    /// <summary>
    ///     Gets the number of acknowledgements that the leader broker must receive before responding to the request.
    ///     The default is <see cref="Kafka.Acks.All" />.
    /// </summary>
    public Acks? Acks { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the producer ensures that messages are written exactly once and in the
    ///     original order. When not set, the idempotence is enabled (Dekaf default), unless <see cref="Acks" /> is
    ///     set to a value other than <see cref="Kafka.Acks.All" /> and the producer isn't transactional, since the
    ///     idempotence requires all acks.
    /// </summary>
    public bool? EnableIdempotence { get; init; }

    /// <summary>
    ///     Gets the delay in milliseconds to wait for messages to accumulate before sending a batch. The value is
    ///     truncated to whole milliseconds and the default is 0 (Dekaf default, unlike the 5 milliseconds of librdkafka).
    ///     Dekaf rejects a linger exceeding the delivery timeout (120 seconds) minus the request timeout (30 seconds)
    ///     when the producer is created.
    /// </summary>
    public double? LingerMs { get; init; }

    /// <summary>
    ///     Gets the transactional id, which enables the transactional producer.
    /// </summary>
    public string? TransactionalId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the producer has to be disposed and recreated if a <c>Dekaf.Errors.KafkaException</c>
    ///     is thrown. The default is <c>true</c>.
    /// </summary>
    public bool DisposeOnException { get; init; } = true;

    /// <summary>
    ///     Gets the flush operation timeout. The default is 30 seconds.
    /// </summary>
    public TimeSpan FlushTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets the transactions init operation timeout. The default is 30 seconds.
    /// </summary>
    public TimeSpan TransactionsInitTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets the transaction commit operation timeout. The default is 30 seconds.
    /// </summary>
    public TimeSpan TransactionCommitTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets the transaction abort operation timeout. The default is 30 seconds.
    /// </summary>
    public TimeSpan TransactionAbortTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets the timeout for sending the offset to the transaction. The default is 10 seconds.
    /// </summary>
    public TimeSpan SendOffsetToTransactionTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Gets the configured endpoints.
    /// </summary>
    public IValueReadOnlyCollection<KafkaProducerEndpointConfiguration> Endpoints { get; init; } = ValueReadOnlyCollection.Empty<KafkaProducerEndpointConfiguration>();

    /// <summary>
    ///     Gets a value indicating whether a <see cref="TransactionalId" /> has been specified.
    /// </summary>
    internal bool IsTransactional => !string.IsNullOrEmpty(TransactionalId);

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (Endpoints == null || Endpoints.Count == 0)
            throw new BrokerConfigurationException("At least one endpoint must be configured.");

        Endpoints.ForEach(endpoint => endpoint.Validate());

        if (FlushTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(FlushTimeout)} must be greater than zero.");

        if (TransactionsInitTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(TransactionsInitTimeout)} must be greater than zero.");

        if (TransactionCommitTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(TransactionCommitTimeout)} must be greater than zero.");

        if (TransactionAbortTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(TransactionAbortTimeout)} must be greater than zero.");

        if (SendOffsetToTransactionTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException($"{nameof(SendOffsetToTransactionTimeout)} must be greater than zero.");

        if (LingerMs < 0)
            throw new BrokerConfigurationException($"{nameof(LingerMs)} must be greater or equal to zero.");

        if (EnableIdempotence == true && Acks is not null and not Kafka.Acks.All)
            throw new BrokerConfigurationException($"The idempotence requires {nameof(Acks)} to be set to {nameof(Kafka.Acks.All)}.");

        if (IsTransactional && EnableIdempotence == false)
            throw new BrokerConfigurationException("The transactions require the idempotence to be enabled.");

        if (IsTransactional && Acks is not null and not Kafka.Acks.All)
            throw new BrokerConfigurationException($"The transactions require {nameof(Acks)} to be set to {nameof(Kafka.Acks.All)}.");
    }
}
