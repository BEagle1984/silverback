// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> and contains the properties shared between the
///     <see cref="KafkaProducerConfiguration" /> and <see cref="KafkaConsumerConfiguration" />.
/// </summary>
/// <typeparam name="TClientConfig">
///     The type of the wrapped <see cref="ClientConfig" />.
/// </typeparam>
public abstract partial record KafkaClientConfiguration<TClientConfig> : IValidatableSettings
    where TClientConfig : ClientConfig, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfiguration{TClientConfig}" /> class.
    /// </summary>
    /// <param name="original">
    ///     The <see cref="KafkaClientConfiguration{TClientConfig}" /> to be cloned.
    /// </param>
    protected KafkaClientConfiguration(KafkaClientConfiguration<TClientConfig> original)
    {
        ClientConfig = Check.NotNull(original, nameof(original)).ClientConfig.Clone();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfiguration{TClientConfig}" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="ClientConfig" /> to be wrapped.
    /// </param>
    protected KafkaClientConfiguration(TClientConfig? clientConfig = null)
    {
        ClientConfig = clientConfig?.Clone() ?? new TClientConfig();
    }

    /// <summary>
    ///     Gets the wrapped <see cref="ClientConfig" />.
    /// </summary>
    protected TClientConfig ClientConfig { get; }

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public abstract void Validate();

    internal virtual TClientConfig GetConfluentClientConfig() => ClientConfig;
}
