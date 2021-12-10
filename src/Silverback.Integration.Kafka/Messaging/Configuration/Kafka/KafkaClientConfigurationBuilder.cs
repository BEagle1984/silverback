// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaClientConfiguration" />.
/// </summary>
public partial class KafkaClientConfigurationBuilder : KafkaClientConfigurationBuilder<ClientConfig, KafkaClientConfigurationBuilder>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    public KafkaClientConfigurationBuilder(KafkaClientConfiguration? clientConfiguration = null)
        : base(clientConfiguration)
    {
    }

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}.This" />
    protected override KafkaClientConfigurationBuilder This => this;

    /// <summary>
    ///     Builds the <see cref="KafkaClientConsumerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientConsumerConfiguration" />.
    /// </returns>
    public KafkaClientConfiguration Build() => new(ClientConfig.Clone());
}
