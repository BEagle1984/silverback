// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The base class for all Kafka client configuration builders.
/// </summary>
/// <typeparam name="TClientConfig">
///     The type of the <see cref="Confluent.Kafka.ClientConfig"/> being built.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract partial class KafkaClientConfigurationBuilder<TClientConfig, TBuilder>
    where TClientConfig : ClientConfig, new()
    where TBuilder : KafkaClientConfigurationBuilder<TClientConfig, TBuilder>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration"/> to be used to initialize the builder.
    /// </param>
    protected KafkaClientConfigurationBuilder(KafkaClientConfiguration? clientConfiguration = null)
    {
        ClientConfig = clientConfiguration?.GetConfluentClientConfig().CloneAs<TClientConfig>() ?? new TClientConfig();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration{TClientConfig}"/> to be used to initialize the builder.
    /// </param>
    protected KafkaClientConfigurationBuilder(KafkaClientConfiguration<TClientConfig> clientConfiguration)
    {
        Check.NotNull(clientConfiguration, nameof(clientConfiguration));

        ClientConfig = clientConfiguration.GetConfluentClientConfig().Clone();
    }

    /// <summary>
    ///     Gets the <see cref="Confluent.Kafka.ClientConfig" /> being wrapped.
    /// </summary>
    protected TClientConfig ClientConfig { get; }

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }
}
