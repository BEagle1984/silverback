// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    internal class ConfluentProducersCache : IConfluentProducersCache
    {
        private readonly ConcurrentDictionary<ProducerConfig, IProducer<byte[]?, byte[]?>> _producersCache =
            new(new ConfigurationDictionaryEqualityComparer<string, string>());

        private readonly IBrokerCallbacksInvoker _callbacksInvoker;

        private readonly IServiceProvider _serviceProvider;

        private readonly ISilverbackLogger _logger;

        public ConfluentProducersCache(
            IServiceProvider serviceProvider,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger<ConfluentProducersCache> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _callbacksInvoker = callbacksInvoker;
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

            producer.Flush(config.FlushTimeout);
            producer.Dispose();
        }

        private IProducer<byte[]?, byte[]?> CreateConfluentProducer(
            KafkaProducerConfig config,
            KafkaProducer ownerProducer)
        {
            _logger.LogCreatingConfluentProducer(ownerProducer);

            var builder = _serviceProvider.GetRequiredService<IConfluentProducerBuilder>();
            builder.SetConfig(config.GetConfluentConfig());
            builder.SetEventsHandlers(ownerProducer, _callbacksInvoker, _logger);
            return builder.Build();
        }
    }
}
