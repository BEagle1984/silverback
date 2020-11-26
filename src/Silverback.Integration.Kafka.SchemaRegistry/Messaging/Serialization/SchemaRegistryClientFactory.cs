// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    internal static class SchemaRegistryClientFactory
    {
        private static readonly ConfigurationDictionaryEqualityComparer<string, string>
            ConfluentConfigEqualityComparer = new();

        private static readonly ConcurrentDictionary<SchemaRegistryConfig, ISchemaRegistryClient> Clients =
            new(ConfluentConfigEqualityComparer);

        public static ISchemaRegistryClient GetClient(SchemaRegistryConfig config) =>
            Clients.GetOrAdd(config, _ => new CachedSchemaRegistryClient(config));
    }
}
