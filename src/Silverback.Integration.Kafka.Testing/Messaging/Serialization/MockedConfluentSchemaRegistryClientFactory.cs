// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal class MockedConfluentSchemaRegistryClientFactory : IConfluentSchemaRegistryClientFactory
{
    private readonly ConcurrentDictionary<string, ISchemaRegistryClient> _clients = new();

    public ISchemaRegistryClient GetClient(Action<KafkaSchemaRegistryConfigurationBuilder> builderAction)
    {
        Check.NotNull(builderAction, nameof(builderAction));

        KafkaSchemaRegistryConfigurationBuilder builder = new();
        builderAction.Invoke(builder);

        return GetClient(builder.Build());
    }

    public ISchemaRegistryClient GetClient(KafkaSchemaRegistryConfiguration schemaRegistryConfiguration)
    {
        Check.NotNull(schemaRegistryConfiguration, nameof(schemaRegistryConfiguration));

        return _clients.GetOrAdd(
            schemaRegistryConfiguration.Url ?? string.Empty,
            static _ => new MockedConfluentSchemaRegistryClient());
    }
}
