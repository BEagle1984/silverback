// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal class ConfluentSchemaRegistryClientFactory : IConfluentSchemaRegistryClientFactory
{
    private static readonly ConcurrentDictionary<KafkaSchemaRegistryConfiguration, ISchemaRegistryClient> Clients = new();

    public ISchemaRegistryClient GetClient(Action<KafkaSchemaRegistryConfigurationBuilder> builderAction)
    {
        Check.NotNull(builderAction, nameof(builderAction));

        KafkaSchemaRegistryConfigurationBuilder builder = new();
        builderAction.Invoke(builder);

        return GetClient(builder.Build());
    }

    public ISchemaRegistryClient GetClient(KafkaSchemaRegistryConfiguration schemaRegistryConfiguration) =>
        Clients.GetOrAdd(
            schemaRegistryConfiguration,
            static configuration => new CachedSchemaRegistryClient(configuration.ToConfluentConfig()));
}
