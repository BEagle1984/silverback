// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    internal static class SchemaRegistryClientFactory
    {
        private static readonly ConfigurationDictionaryComparer<string, string> ConfluentConfigComparer =
            new ConfigurationDictionaryComparer<string, string>();

        private static readonly ConcurrentDictionary<SchemaRegistryConfig, ISchemaRegistryClient> Clients =
            new ConcurrentDictionary<SchemaRegistryConfig, ISchemaRegistryClient>(ConfluentConfigComparer);

        public static ISchemaRegistryClient GetClient(SchemaRegistryConfig config) =>
            Clients.GetOrAdd(config, _ => new CachedSchemaRegistryClient(config));
    }
}