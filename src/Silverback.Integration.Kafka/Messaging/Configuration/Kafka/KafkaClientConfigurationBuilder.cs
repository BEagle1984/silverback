// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaClientConfiguration" />.
/// </summary>
public partial class KafkaClientConfigurationBuilder
{
    private readonly ClientConfig _clientConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    public KafkaClientConfigurationBuilder(KafkaClientConfiguration? clientConfig = null)
        : this(clientConfig?.GetConfluentClientConfig().Clone() ?? new ClientConfig())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="ClientConfig" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    protected internal KafkaClientConfigurationBuilder(ClientConfig clientConfig)
        : base(clientConfig)
    {
        _clientConfig = clientConfig;
    }

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{ClientConfig}.This" />
    protected override KafkaClientConfigurationBuilder This => this;

    /// <summary>
    ///     Builds the <see cref="KafkaClientConsumerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientConsumerConfiguration" />.
    /// </returns>
    public KafkaClientConfiguration Build() => new(_clientConfig);
}
