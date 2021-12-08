// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaClientConsumerConfiguration" />.
/// </summary>
public partial class KafkaClientConsumerConfigurationBuilder
{
    private readonly ConsumerConfig _clientConfig;

    private int? _commitOffsetEach;

    private bool? _enableAutoRecovery;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConsumerConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientConsumerConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    public KafkaClientConsumerConfigurationBuilder(KafkaClientConsumerConfiguration? clientConfig = null)
        : this(clientConfig?.GetConfluentClientConfig() ?? new ClientConfig())
    {
        // TODO: test to ensure we don't forget any assignment
        _commitOffsetEach = clientConfig?.CommitOffsetEach;
        _enableAutoRecovery = clientConfig?.EnableAutoRecovery;
    }

    internal KafkaClientConsumerConfigurationBuilder(ClientConfig clientConfig)
        : this(new ConsumerConfig(clientConfig.Clone()))
    {
    }

    private KafkaClientConsumerConfigurationBuilder(ConsumerConfig clientConfig)
        : base(clientConfig)
    {
        _clientConfig = clientConfig;
    }

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{ClientConfig}.This" />
    protected override KafkaClientConsumerConfigurationBuilder This => this;

    /// <summary>
    ///     Defines the number of message to be processed before committing the offset to the server. The most
    ///     reliable level is 1 but it reduces throughput.
    /// </summary>
    /// <remarks>
    ///     Setting this value automatically disables automatic offset commit (see <see cref="EnableAutoCommit" />/<see cref="DisableAutoCommit" />).
    /// </remarks>
    /// <param name="commitOffsetEach">
    ///     The number of message to be processed before committing the offset to the server.
    /// </param>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder CommitOffsetEach(int commitOffsetEach)
    {
        _commitOffsetEach = commitOffsetEach;
        WithEnableAutoCommit(false);
        return this;
    }

    /// <summary>
    ///     Specifies that the consumer has to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). This is the default.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder EnableAutoRecovery()
    {
        _enableAutoRecovery = true;
        return this;
    }

    /// <summary>
    ///     Specifies that the consumer doesn't have to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported).
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder DisableAutoRecovery()
    {
        _enableAutoRecovery = false;
        return this;
    }

    /// <summary>
    ///     Automatically and periodically commit offsets in the background.
    /// </summary>
    /// <remarks>
    ///     Enabling automatic offsets commit will clear the <see cref="KafkaClientConsumerConfiguration.CommitOffsetEach" /> setting
    ///     (see <see cref="CommitOffsetEach" />).
    /// </remarks>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder EnableAutoCommit()
    {
        _commitOffsetEach = null;
        return WithEnableAutoCommit(true);
    }

    /// <summary>
    ///     Disable automatic offsets commit. Note: setting this does not prevent the consumer from fetching previously committed start
    ///     offsets. To circumvent this behaviour set specific start offsets per partition in the call to assign().
    /// </summary>
    /// <remarks>
    ///     See also <see cref="CommitOffsetEach" />.
    /// </remarks>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder DisableAutoCommit() => WithEnableAutoCommit(false);

    /// <summary>
    ///     Invoke the <see cref="IKafkaPartitionEofCallback" /> whenever a partition end of file is reached.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder EnablePartitionEof() => WithEnablePartitionEof(true);

    /// <summary>
    ///     Don't invoke the <see cref="IKafkaPartitionEofCallback" /> when a partition end of file is reached.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder DisablePartitionEof() => WithEnablePartitionEof(false);

    /// <summary>
    ///     Allow automatic topic creation on the broker when subscribing to or assigning non-existent topics. T
    ///     he broker must also be configured with `auto.create.topics.enable=true` for this configuration to take effect.
    ///     Note: The default value (false) is different from the Java consumer (true).
    ///     Requires broker version &gt;= 0.11.0.0, for older broker versions only the broker configuration applies.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder AllowAutoCreateTopics() => WithAllowAutoCreateTopics(true);

    /// <summary>
    ///     Disallow automatic topic creation on the broker when subscribing to or assigning non-existent topics.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaClientConsumerConfigurationBuilder DisallowAutoCreateTopics() => WithAllowAutoCreateTopics(false);

    /// <summary>
    ///     Builds the <see cref="KafkaClientConsumerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientConsumerConfiguration" />.
    /// </returns>
    public KafkaClientConsumerConfiguration Build()
    {
        KafkaClientConsumerConfiguration config = new(_clientConfig);

        return config with
        {
            CommitOffsetEach = _commitOffsetEach ?? config.CommitOffsetEach,
            EnableAutoRecovery = _enableAutoRecovery ?? config.EnableAutoRecovery
        };
    }
}
