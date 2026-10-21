// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The base class for the builders of the <see cref="KafkaClientConfiguration" /> derived types, holding the
///     settings shared by producers and consumers.
/// </summary>
/// <typeparam name="TConfig">
///     The type of the configuration being built.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract class KafkaClientConfigurationBuilder<TConfig, TBuilder> : IKafkaClientConfigurationBuilder
    where TConfig : KafkaClientConfiguration, new()
    where TBuilder : KafkaClientConfigurationBuilder<TConfig, TBuilder>
{
    private string? _bootstrapServers;

    private string? _clientId;

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Sets the comma-separated list of brokers (host or host:port).
    /// </summary>
    /// <param name="bootstrapServers">
    ///     The comma-separated list of brokers.
    /// </param>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithBootstrapServers(string? bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
        return This;
    }

    /// <summary>
    ///     Sets the client identifier, sent to the brokers with every request.
    /// </summary>
    /// <param name="clientId">
    ///     The client identifier.
    /// </param>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithClientId(string? clientId)
    {
        _clientId = clientId;
        return This;
    }

    /// <inheritdoc cref="WithBootstrapServers" />
    void IKafkaClientConfigurationBuilder.WithBootstrapServers(string? bootstrapServers) => WithBootstrapServers(bootstrapServers);

    /// <inheritdoc cref="WithClientId" />
    void IKafkaClientConfigurationBuilder.WithClientId(string? clientId) => WithClientId(clientId);

    /// <summary>
    ///     Builds the configuration applying the shared settings.
    /// </summary>
    /// <returns>
    ///     The configuration.
    /// </returns>
    protected virtual TConfig BuildCore() =>
        new()
        {
            BootstrapServers = _bootstrapServers,
            ClientId = _clientId
        };
}
