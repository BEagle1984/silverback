// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds an <see cref="IMessageKeyDeserializer" /> based on the schema registry.
/// </summary>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract class SchemaRegistryKeyDeserializerBuilder<TBuilder>
    : SchemaRegistryBuilder<TBuilder>
    where TBuilder : SchemaRegistryKeyDeserializerBuilder<TBuilder>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryKeyDeserializerBuilder{TBuilder}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    protected SchemaRegistryKeyDeserializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <summary>
    ///     Builds the <see cref="IMessageKeyDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageKeyDeserializer" />.
    /// </returns>
    public IMessageKeyDeserializer Build()
    {
        ISchemaRegistryClient client = BuildSchemaRegistryClient();
        return BuildCore(client);
    }

    /// <summary>
    ///     Builds the <see cref="IMessageKeyDeserializer" /> instance.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <returns>
    ///     The <see cref="IMessageKeyDeserializer" />.
    /// </returns>
    protected abstract IMessageKeyDeserializer BuildCore(ISchemaRegistryClient schemaRegistryClient);
}
