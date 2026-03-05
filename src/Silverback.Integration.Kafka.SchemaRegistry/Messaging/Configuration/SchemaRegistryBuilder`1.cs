// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Base class for schema registry builders, providing common configuration methods
///     for specifying the schema registry connection.
/// </summary>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract class SchemaRegistryBuilder<TBuilder>
    where TBuilder : SchemaRegistryBuilder<TBuilder>
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory;
    private KafkaSchemaRegistryConfiguration? _schemaRegistryConfiguration = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryBuilder{TBuilder}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    protected SchemaRegistryBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
    {
        _schemaRegistryClientFactory = schemaRegistryClientFactory;
    }

    /// <summary>
    ///     Gets the actual builder instance.
    /// </summary>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Configures the <see cref="SchemaRegistryConfig" />.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="SchemaRegistryConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ConnectToSchemaRegistry(Action<KafkaSchemaRegistryConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));
        KafkaSchemaRegistryConfigurationBuilder builder = new();
        configurationBuilderAction.Invoke(builder);
        _schemaRegistryConfiguration = builder.Build();
        return This;
    }

    /// <summary>
    ///     Configures the <see cref="SchemaRegistryConfig" />.
    /// </summary>
    /// <param name="url">
    ///     The comma-separated list of URLs for schema registry instances.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="SchemaRegistryConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Declared as string in the underlying library")]
    public TBuilder ConnectToSchemaRegistry(string url, Action<KafkaSchemaRegistryConfigurationBuilder>? configurationBuilderAction = null)
    {
        KafkaSchemaRegistryConfigurationBuilder builder = new();
        builder.WithUrl(url);
        configurationBuilderAction?.Invoke(builder);
        _schemaRegistryConfiguration = builder.Build();
        return This;
    }

    /// <summary>
    ///     Validates the configuration and creates the schema registry client.
    /// </summary>
    /// <returns>
    ///     A tuple containing the message type and the <see cref="ISchemaRegistryClient" />.
    /// </returns>
    protected ISchemaRegistryClient BuildSchemaRegistryClient()
    {
        if (_schemaRegistryConfiguration == null)
            throw new SilverbackConfigurationException("The schema registry configuration was not specified. Please call ConnectToSchemaRegistry.");

        _schemaRegistryConfiguration.Validate();

        return _schemaRegistryClientFactory.GetClient(_schemaRegistryConfiguration);
    }
}
