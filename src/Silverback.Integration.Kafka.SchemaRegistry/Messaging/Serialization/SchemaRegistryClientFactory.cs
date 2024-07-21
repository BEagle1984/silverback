// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

namespace Silverback.Messaging.Serialization;

internal class SchemaRegistryClientFactory : ISchemaRegistryClientFactory
{
    private static readonly ConcurrentDictionary<KafkaSchemaRegistryConfiguration, ISchemaRegistryClient> Clients = new();

    public ISchemaRegistryClient GetClient(KafkaSchemaRegistryConfiguration schemaRegistryConfiguration) =>
        Clients.GetOrAdd(
            schemaRegistryConfiguration,
            static configuration => new CachedSchemaRegistryClient(configuration.GetConfluentSchemaRegistryConfig()));
}
