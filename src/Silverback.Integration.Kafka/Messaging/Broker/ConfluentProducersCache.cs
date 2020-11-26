// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.KafkaEvents;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal class ConfluentProducersCache : IConfluentProducersCache
    {
        private readonly ConcurrentDictionary<ProducerConfig, IProducer<byte[]?, byte[]?>> _producersCache =
            new(new ConfigurationDictionaryEqualityComparer<string, string>());

        private readonly IServiceProvider _serviceProvider;

        private readonly ISilverbackIntegrationLogger<ConfluentProducersCache> _logger;

        public ConfluentProducersCache(
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<ConfluentProducersCache> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IProducer<byte[]?, byte[]?> GetProducer(KafkaProducerConfig config, KafkaProducer owner) =>
            _producersCache.GetOrAdd(
                config.GetConfluentConfig(),
                _ => CreateConfluentProducer(config, owner));

        public void DisposeProducer(KafkaProducerConfig config)
        {
            // Dispose only if still in cache to avoid ObjectDisposedException
            if (!_producersCache.TryRemove(config.GetConfluentConfig(), out var producer))
                return;

            producer.Flush(TimeSpan.FromSeconds(10));
            producer.Dispose();
        }

        private IProducer<byte[]?, byte[]?> CreateConfluentProducer(KafkaProducerConfig config, KafkaProducer owner)
        {
            _logger.LogDebug(KafkaEventIds.CreatingConfluentProducer, "Creating Confluent.Kafka.Producer...");

            var builder = _serviceProvider.GetRequiredService<IConfluentProducerBuilder>();
            builder.SetConfig(config.GetConfluentConfig());
            builder.SetEventsHandlers(owner, _logger);
            return builder.Build();
        }
    }
}
