// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The settings shared by the Kafka producers and consumers. Only the subset of settings that is mapped to the
///     Dekaf client is exposed.
/// </summary>
public abstract record KafkaClientConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the comma-separated list of brokers (host or host:port).
    /// </summary>
    public string? BootstrapServers { get; init; }

    /// <summary>
    ///     Gets the client identifier, sent to the brokers with every request.
    /// </summary>
    public string? ClientId { get; init; }

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public virtual void Validate()
    {
        if (string.IsNullOrEmpty(BootstrapServers))
            throw new BrokerConfigurationException($"The {nameof(BootstrapServers)} are required to connect with the message broker.");
    }
}
