// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> and contains the properties shared between the
///     <see cref="KafkaClientProducerConfiguration" /> and <see cref="KafkaClientConsumerConfiguration" />.
/// </summary>
/// <typeparam name="TClientConfig">
///     The type of the wrapped <see cref="ClientConfig" />.
/// </typeparam>
public abstract partial record KafkaClientConfiguration<TClientConfig> : IValidatableEndpointSettings
    where TClientConfig : ClientConfig, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfiguration{TClientConfig}" /> class.
    /// </summary>
    /// <param name="original">
    ///     The <see cref="KafkaClientConfiguration{TClientConfig}" /> to be cloned.
    /// </param>
    protected KafkaClientConfiguration(KafkaClientConfiguration<TClientConfig>? original)
    {
        ClientConfig = original?.ClientConfig.Clone() ?? new TClientConfig();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfiguration{TClientConfig}" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="ClientConfig" /> to be wrapped.
    /// </param>
    protected KafkaClientConfiguration(TClientConfig clientConfig)
    {
        ClientConfig = clientConfig;
    }

    /// <summary>
    ///     Gets the wrapped <see cref="ClientConfig" />.
    /// </summary>
    protected TClientConfig ClientConfig { get; }

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public abstract void Validate();

    internal TClientConfig GetConfluentClientConfig() => ClientConfig;
}
