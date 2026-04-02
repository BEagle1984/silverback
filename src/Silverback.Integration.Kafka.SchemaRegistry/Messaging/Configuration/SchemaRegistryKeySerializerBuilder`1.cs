// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds an <see cref="IMessageKeySerializer" /> based on the schema registry.
/// </summary>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract class SchemaRegistryKeySerializerBuilder<TBuilder> : SchemaRegistryBuilder<TBuilder>
    where TBuilder : SchemaRegistryKeySerializerBuilder<TBuilder>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryKeySerializerBuilder{TBuilder}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    protected SchemaRegistryKeySerializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <summary>
    ///     Builds the <see cref="IMessageKeySerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageKeySerializer" />.
    /// </returns>
    public IMessageKeySerializer Build()
    {
        ISchemaRegistryClient client = BuildSchemaRegistryClient();
        return BuildCore(client);
    }

    /// <summary>
    ///     Builds the <see cref="IMessageKeySerializer" /> instance.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <returns>
    ///     The <see cref="IMessageKeySerializer" />.
    /// </returns>
    protected abstract IMessageKeySerializer BuildCore(ISchemaRegistryClient schemaRegistryClient);
}
