// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds an <see cref="IMessageSerializer" /> based on the schema registry.
/// </summary>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract class SchemaRegistrySerializerBuilder<TBuilder>
    where TBuilder : SchemaRegistrySerializerBuilder<TBuilder>
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory;

    private Type? _messageType;

    private KafkaSchemaRegistryConfiguration? _schemaRegistryConfiguration = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistrySerializerBuilder{TBuilder}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    protected SchemaRegistrySerializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
    {
        _schemaRegistryClientFactory = schemaRegistryClientFactory;
    }

    /// <summary>
    ///     Gets the actual builder instance.
    /// </summary>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Specifies the message type.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize.
    /// </typeparam>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder UseModel<TMessage>()
        where TMessage : class
    {
        _messageType = typeof(TMessage);
        return This;
    }

    /// <summary>
    ///     Specifies the message type.
    /// </summary>
    /// <param name="messageType">
    ///     The type of the message to serialize or serialize.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder UseModel(Type messageType)
    {
        _messageType = Check.NotNull(messageType, nameof(messageType));
        return This;
    }

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
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    public IMessageSerializer Build()
    {
        if (_messageType == null)
            throw new SilverbackConfigurationException("The message type was not specified. Please call UseModel.");

        if (_schemaRegistryConfiguration == null)
            throw new SilverbackConfigurationException("The schema registry configuration was not specified. Please call ConnectToSchemaRegistry.");

        _schemaRegistryConfiguration.Validate();

        return BuildCore(_messageType, _schemaRegistryClientFactory.GetClient(_schemaRegistryConfiguration));
    }

    /// <summary>
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <param name="messageType">
    ///     The type of the message to serialize.
    /// </param>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    protected abstract IMessageSerializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient);
}
