// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The base class for all Kafka client configuration builders.
/// </summary>
/// <typeparam name="TClientConfig">
///     The type of the <see cref="Confluent.Kafka.ClientConfig" /> being built.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract partial class KafkaClientConfigurationBuilder<TClientConfig, TBuilder>
    where TClientConfig : ClientConfig, new()
    where TBuilder : KafkaClientConfigurationBuilder<TClientConfig, TBuilder>
{
    /// <summary>
    ///     Gets the <see cref="Confluent.Kafka.ClientConfig" /> being wrapped.
    /// </summary>
    protected TClientConfig ClientConfig { get; } = new();

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }
}
