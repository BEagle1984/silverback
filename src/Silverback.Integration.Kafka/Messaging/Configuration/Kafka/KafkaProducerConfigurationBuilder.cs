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

    private TimeSpan? _transactionsInitTimeout;

    private TimeSpan? _transactionCommitTimeout;

    private TimeSpan? _transactionAbortTimeout;

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}.This" />
    protected override KafkaProducerConfigurationBuilder This => this;

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
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
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
        _flushTimeout = timeout;
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
        _transactionsInitTimeout = timeout;
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
        _transactionCommitTimeout = timeout;
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
        _transactionAbortTimeout = timeout;
        return this;
    }

    /// <summary>
    ///     Enable notification of delivery reports.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableDeliveryReports()
    {
        ClientConfig.EnableDeliveryReports = true;
        return this;
    }

    /// <summary>
    ///     Disable notification of delivery reports. This will enable "fire and forget" semantics and give a small boost in performance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableDeliveryReports()
    {
        ClientConfig.EnableDeliveryReports = false;
        return this;
    }

    /// <summary>
    ///     The producer will ensure that messages are successfully produced exactly once and in the original produce order.
    ///     The following configuration properties are adjusted automatically (if not modified by the user) when idempotence is enabled:
    ///     `max.in.flight.requests.per.connection=5` (must be less than or equal to 5), `retries=INT32_MAX` (must be greater than 0),
    ///     `acks=all`, `queuing.strategy=fifo`. Producer instantiation will fail if user-supplied configuration is incompatible.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableIdempotence()
    {
        ClientConfig.EnableIdempotence = true;
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
        ClientConfig.EnableIdempotence = false;
        return this;
    }

    /// <summary>
    ///     Specifies that an error that could result in a gap in the produced message series when a batch of messages fails, must raise a
    ///     fatal error (ERR_GAPLESS_GUARANTEE) and stop the producer. Messages failing due to <see cref="KafkaProducerConfiguration.MessageTimeoutMs" />
    ///     are not covered by this guarantee. Requires <see cref="EnableIdempotence" />=true.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder EnableGaplessGuarantee()
    {
        WithEnableGaplessGuarantee(true);
        return this;
    }

    /// <summary>
    ///     Disables the gapless guarantee.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaProducerConfigurationBuilder DisableGaplessGuarantee()
    {
        WithEnableGaplessGuarantee(false);
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
    public KafkaProducerConfigurationBuilder DisableTransactions() =>
        WithTransactionalId(null);

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
            TransactionsInitTimeout = _transactionsInitTimeout ?? configuration.TransactionsInitTimeout,
            TransactionCommitTimeout = _transactionCommitTimeout ?? configuration.TransactionCommitTimeout,
            TransactionAbortTimeout = _transactionAbortTimeout ?? configuration.TransactionAbortTimeout,
            Endpoints = _endpoints.Values.AsValueReadOnlyCollection()
        };

        configuration.Validate();

        return configuration;
    }

    /// <summary>
    ///     Sets the ack timeout of the producer request in milliseconds. This value is only enforced by the broker and relies on
    ///     <c>request.required.acks</c> being != 0.
    /// </summary>
    /// <param name="requestTimeoutMs">
    ///     The ack timeout of the producer request.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithRequestTimeoutMs(int? requestTimeoutMs);

    /// <summary>
    ///     Sets the local message timeout (in milliseconds). This value is only enforced locally and limits the time a produced message waits
    ///     for successful delivery. A time of 0 is infinite. This is the maximum time to deliver a message (including retries) and a delivery
    ///     error will occur when either the retry count or the message timeout are exceeded. The message timeout is automatically adjusted to
    ///     <see cref="KafkaProducerConfiguration.TransactionTimeoutMs" /> if  <see cref="KafkaProducerConfiguration.TransactionalId" /> is set.
    /// </summary>
    /// <param name="messageTimeoutMs">
    ///     The local message timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithMessageTimeoutMs(int? messageTimeoutMs);

    /// <summary>
    ///     Sets the partitioner to be used to decide the target partition for a message: <see cref="Confluent.Kafka.Partitioner.Random" />
    ///     to randomly distribute the messages, <see cref="Confluent.Kafka.Partitioner.Consistent" /> to use the CRC32 hash of the message key
    ///     (empty and null keys are mapped to a single partition), <see cref="Confluent.Kafka.Partitioner.ConsistentRandom" /> to use the CRC32
    ///     hash of the message key (but empty and null keys are randomly partitioned), <see cref="Confluent.Kafka.Partitioner.Murmur2" /> to use
    ///     a Java Producer compatible Murmur2 hash of the message key (null keys are mapped to a single partition), or
    ///     <see cref="Confluent.Kafka.Partitioner.Murmur2Random" /> to use a Java Producer compatible Murmur2 hash of the message key (but null
    ///     keys are randomly partitioned).<br />
    ///     The default is <see cref="Confluent.Kafka.Partitioner.ConsistentRandom" />, while <see cref="Confluent.Kafka.Partitioner.Murmur2Random" />
    ///     is functionally equivalent to the default partitioner in the Java Producer.
    /// </summary>
    /// <param name="partitioner">
    ///     The partitioner to be used to decide the target partition for a message.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithPartitioner(Partitioner? partitioner);

    /// <summary>
    ///     Sets the compression level parameter for the algorithm selected by configuration property <see cref="CompressionType" />. Higher
    ///     values will result in better compression at the cost of higher CPU usage. Usable range is algorithm-dependent: [0-9] for gzip,
    ///     [0-12] for lz4, only 0 for snappy. -1 = codec-dependent default compression level.
    /// </summary>
    /// <param name="compressionLevel">
    ///     The compression level parameter for the algorithm selected by configuration property <see cref="CompressionType" />.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithCompressionLevel(int? compressionLevel);

    /// <summary>
    ///     Sets the maximum amount of time in milliseconds that the transaction coordinator will wait for a transaction status update from
    ///     the producer before proactively aborting the ongoing transaction. If this value is larger than the <c>transaction.max.timeout.ms</c>
    ///     setting in the broker, the init transaction call will fail with ERR_INVALID_TRANSACTION_TIMEOUT. The transaction timeout automatically
    ///     adjusts <see cref="KafkaProducerConfiguration.MessageTimeoutMs" /> and <see cref="KafkaClientConfiguration{TClientConfig}.SocketTimeoutMs" /> unless explicitly configured in which case
    ///     they must not exceed the transaction timeout (<see cref="KafkaClientConfiguration{TClientConfig}.SocketTimeoutMs" /> must be at least 100ms lower than
    ///     <see cref="KafkaProducerConfiguration.TransactionTimeoutMs" />).
    /// </summary>
    /// <param name="transactionTimeoutMs">
    ///     The maximum amount of time that the transaction coordinator will wait for a transaction status update from the producer before
    ///     proactively aborting the ongoing transaction.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithTransactionTimeoutMs(int? transactionTimeoutMs);

    /// <summary>
    ///     Sets the maximum number of messages allowed on the producer queue. This queue is shared by all topics and partitions.
    /// </summary>
    /// <param name="queueBufferingMaxMessages">
    ///     The maximum number of messages allowed on the producer queue.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithQueueBufferingMaxMessages(int? queueBufferingMaxMessages);

    /// <summary>
    ///     Sets the maximum total message size sum allowed on the producer queue. This queue is shared by all topics and partitions. This
    ///     property has higher priority than <see cref="KafkaProducerConfiguration.QueueBufferingMaxMessages" />.
    /// </summary>
    /// <param name="queueBufferingMaxKbytes">
    ///     The maximum total message size sum allowed on the producer queue.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithQueueBufferingMaxKbytes(int? queueBufferingMaxKbytes);

    /// <summary>
    ///     Sets the delay in milliseconds to wait for messages in the producer queue to accumulate before constructing message batches to
    ///     transmit to brokers. A higher value allows larger and more effective (less overhead, improved compression) batches of messages to
    ///     accumulate at the expense of increased message delivery latency.
    /// </summary>
    /// <param name="lingerMs">
    ///     The delay to wait for messages in the producer queue to accumulate before constructing message batches.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithLingerMs(double? lingerMs);

    /// <summary>
    ///     Sets how many times to retry sending a failing message.<br />
    ///     Note: retrying may cause reordering unless <see cref="EnableIdempotence" /> is set to <c>true</c>.
    /// </summary>
    /// <param name="messageSendMaxRetries">
    ///     How many times to retry sending a failing message.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithMessageSendMaxRetries(int? messageSendMaxRetries);

    /// <summary>
    ///     Sets the backoff time in milliseconds before retrying a request.
    /// </summary>
    /// <param name="retryBackoffMs">
    ///     The backoff time before retrying a request.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithRetryBackoffMs(int? retryBackoffMs);

    /// <summary>
    ///     Sets the threshold of outstanding not yet transmitted broker requests needed to backpressure the producer's message accumulator.
    ///     If the number of not yet transmitted requests equals or exceeds this number, produce request creation that would have otherwise
    ///     been triggered (for example, in accordance with <see cref="KafkaProducerConfiguration.LingerMs" />) will be delayed. A lower number
    ///     yields larger and more effective batches. A higher value can improve latency when using compression on slow machines.
    /// </summary>
    /// <param name="queueBufferingBackpressureThreshold">
    ///     The threshold of outstanding not yet transmitted broker requests.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithQueueBufferingBackpressureThreshold(int? queueBufferingBackpressureThreshold);

    /// <summary>
    ///     Sets the compression codec to be used to compress message sets. This is the default value for all topics, may be overridden by the
    ///     topic configuration property <c>compression.codec</c>.
    /// </summary>
    /// <param name="compressionType">
    ///     The compression codec to be used to compress message sets.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithCompressionType(CompressionType? compressionType);

    /// <summary>
    ///     Sets the maximum number of messages batched in one message set. The total message set size is also limited by
    ///     <see cref="KafkaProducerConfiguration.BatchSize" /> and <see cref="KafkaClientConfiguration{TClientConfig}.MessageMaxBytes" />.
    /// </summary>
    /// <param name="batchNumMessages">
    ///     The maximum number of messages batched in one message set.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithBatchNumMessages(int? batchNumMessages);

    /// <summary>
    ///     Sets the maximum size (in bytes) of all messages batched in one message set, including the protocol framing overhead. This limit
    ///     is applied after the first message has been added to the batch, regardless of the first message size, this is to ensure that messages
    ///     that exceed the <see cref="KafkaProducerConfiguration.BatchSize" /> are still produced. The total message set size is also limited by
    ///     <see cref="KafkaProducerConfiguration.BatchNumMessages" /> and <see cref="KafkaClientConfiguration{TClientConfig}.MessageMaxBytes" />.
    /// </summary>
    /// <param name="batchSize">
    ///     The maximum size of all messages batched in one message set.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithBatchSize(int? batchSize);

    /// <summary>
    ///     Sets the delay in milliseconds to wait to assign new sticky partitions for each topic. By default this is set to double the time
    ///     of <see cref="KafkaProducerConfiguration.LingerMs" />. To disable sticky behavior, set it to 0. This behavior affects messages with
    ///     the key <c>null</c> in all cases, and messages with key lengths of zero when the <see cref="Confluent.Kafka.Partitioner.ConsistentRandom" />
    ///     partitioner is in use. These messages would otherwise be assigned randomly. A higher value allows for more effective batching of
    ///     these messages.
    /// </summary>
    /// <param name="stickyPartitioningLingerMs">
    ///     The delay in milliseconds to wait to assign new sticky partitions for each topic.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaProducerConfigurationBuilder WithStickyPartitioningLingerMs(int? stickyPartitioningLingerMs);
}
