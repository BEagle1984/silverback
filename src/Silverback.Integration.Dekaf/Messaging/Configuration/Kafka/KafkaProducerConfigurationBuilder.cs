// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaProducerConfiguration" />.
/// </summary>
public class KafkaProducerConfigurationBuilder
    : KafkaClientConfigurationBuilder<KafkaProducerConfiguration, KafkaProducerConfigurationBuilder>
{
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, KafkaProducerEndpointConfiguration> _endpoints = [];

    private Acks? _acks;

    private bool? _enableIdempotence;

    private double? _lingerMs;

    private string? _transactionalId;

    private bool? _disposeOnException;

    private TimeSpan? _flushTimeout;

    private TimeSpan? _transactionsInitTimeout;

    private TimeSpan? _transactionCommitTimeout;

    private TimeSpan? _transactionAbortTimeout;

    private TimeSpan? _sendOffsetToTransactionTimeout;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducerConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public KafkaProducerConfigurationBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TConfig,TBuilder}.This" />
    protected override KafkaProducerConfigurationBuilder This => this;

    /// <summary>
    ///     Sets the number of acknowledgements that the leader broker must receive before responding to the request.
    /// </summary>
    /// <param name="acks">
    ///     The <see cref="Acks" />.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithAcks(Acks? acks)
    {
        _acks = acks;
        return this;
    }

    /// <summary>
    ///     Sets the delay in milliseconds to wait for messages to accumulate before sending a batch. The value is
    ///     truncated to whole milliseconds and the default is 0.
    /// </summary>
    /// <param name="lingerMs">
    ///     The linger delay in milliseconds.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithLingerMs(double? lingerMs)
    {
        _lingerMs = lingerMs;
        return this;
    }

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
    ///     By default, the name will be generated concatenating the topic name and the message type.
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
    public KafkaProducerConfigurationBuilder Produce<TMessage>(Action<KafkaProducerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction)
        where TMessage : class =>
        Produce(null, configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic or partition and its related configuration (serializer, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being produced. This is used to set up the serializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default, the name will be generated concatenating the topic name and the message type.
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
        where TMessage : class
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaProducerEndpointConfigurationBuilder<TMessage> builder = new(_serviceProvider, name);
        configurationBuilderAction.Invoke(builder);
        KafkaProducerEndpointConfiguration endpointConfiguration = builder.Build();

        _endpoints.TryAdd(name ?? $"{endpointConfiguration.RawName}|{typeof(TMessage).FullName}", endpointConfiguration);

        return this;
    }

    /// <summary>
    ///     Specifies that the producer has to be disposed and recreated if a <c>Dekaf.Errors.KafkaException</c> is thrown.
    ///     This is the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisposeOnException()
    {
        _disposeOnException = true;
        return this;
    }

    /// <summary>
    ///     Specifies that the producer don't have to be disposed and recreated if a <c>Dekaf.Errors.KafkaException</c> is thrown.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithFlushTimeout(TimeSpan timeout)
    {
        _flushTimeout = Check.GreaterThan(timeout, nameof(timeout), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Specifies the transactions init operation timeout. The default is 30 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The transactions init operation timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithTransactionsInitTimeout(TimeSpan timeout)
    {
        _transactionsInitTimeout = Check.GreaterThan(timeout, nameof(timeout), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Specifies the transaction commit operation timeout. The default is 30 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The transaction commit operation timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithTransactionCommitTimeout(TimeSpan timeout)
    {
        _transactionCommitTimeout = Check.GreaterThan(timeout, nameof(timeout), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Specifies the transaction abort operation timeout. The default is 30 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The transaction abort operation timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithTransactionAbortTimeout(TimeSpan timeout)
    {
        _transactionAbortTimeout = Check.GreaterThan(timeout, nameof(timeout), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     The producer will ensure that messages are successfully produced exactly once and in the original produce order.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableIdempotence()
    {
        WithEnableIdempotence(true);
        return this;
    }

    /// <summary>
    ///     The producer will <b>not</b> ensure that messages are successfully produced exactly once and in the original produce order.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableIdempotence()
    {
        WithEnableIdempotence(false);
        return this;
    }

    /// <summary>
    ///     Enables the Kafka transactions and sets the identifier to be used to identify the same transactional producer instance across
    ///     process restarts. This allows the producer to guarantee that transactions corresponding to earlier instances of the same producer
    ///     have been finalized prior to starting any new transaction, and that any zombie instances are fenced off.
    ///     Requires broker version &gt;= 0.11.0.
    /// </summary>
    /// <param name="transactionalId">
    ///     The identifier to be used to identify the same transactional producer instance across process restarts.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableTransactions(string transactionalId) =>
        WithTransactionalId(Check.NotNullOrEmpty(transactionalId, nameof(transactionalId)));

    /// <summary>
    ///     Disables the Kafka transactions. The producer is limited to idempotent delivery (see <see cref="EnableIdempotence" />).
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableTransactions() => WithTransactionalId(null);

    /// <summary>
    ///     Specifies the timeout for sending the offset to the transaction. The default is 10 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The timeout for sending the offset to the transaction.
    /// </param>
    /// ///
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder WithSendOffsetToTransactionTimeout(TimeSpan timeout)
    {
        _sendOffsetToTransactionTimeout = Check.GreaterThan(timeout, nameof(timeout), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="KafkaProducerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfiguration" />.
    /// </returns>
    public KafkaProducerConfiguration Build()
    {
        KafkaProducerConfiguration configuration = BuildCore();

        configuration = configuration with
        {
            Acks = _acks ?? configuration.Acks,
            EnableIdempotence = _enableIdempotence ?? configuration.EnableIdempotence,
            LingerMs = _lingerMs ?? configuration.LingerMs,
            TransactionalId = _transactionalId ?? configuration.TransactionalId,
            DisposeOnException = _disposeOnException ?? configuration.DisposeOnException,
            FlushTimeout = _flushTimeout ?? configuration.FlushTimeout,
            TransactionsInitTimeout = _transactionsInitTimeout ?? configuration.TransactionsInitTimeout,
            TransactionCommitTimeout = _transactionCommitTimeout ?? configuration.TransactionCommitTimeout,
            TransactionAbortTimeout = _transactionAbortTimeout ?? configuration.TransactionAbortTimeout,
            SendOffsetToTransactionTimeout = _sendOffsetToTransactionTimeout ?? configuration.SendOffsetToTransactionTimeout,
            Endpoints = _endpoints.Values.AsValueReadOnlyCollection()
        };

        configuration.Validate();

        return configuration;
    }

    private KafkaProducerConfigurationBuilder WithEnableIdempotence(bool? enableIdempotence)
    {
        _enableIdempotence = enableIdempotence;
        return this;
    }

    /// <summary>
    ///     Sets the transactional id, which enables the transactional producer.
    /// </summary>
    /// <param name="transactionalId">
    ///     The transactional id.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    private KafkaProducerConfigurationBuilder WithTransactionalId(string? transactionalId)
    {
        _transactionalId = transactionalId;
        return this;
    }
}
