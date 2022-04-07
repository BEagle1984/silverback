// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka
{
    internal sealed class ConfluentProducersCache : IConfluentProducersCache
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

        public IProducer<byte[]?, byte[]?> GetProducer(KafkaProducer ownerProducer)
        {
            if (ownerProducer.Endpoint.Configuration.IsTransactional)
                return CreateConfluentProducer(ownerProducer);

            return _producersCache.GetOrAdd(
                ownerProducer.Endpoint.Configuration.GetConfluentConfig(),
                _ => CreateConfluentProducer(ownerProducer));
        }

        public void DisposeProducer(KafkaProducer ownerProducer)
        {
            // Dispose only if still in cache to avoid ObjectDisposedException
            if (!_producersCache.TryRemove(ownerProducer.Endpoint.Configuration.GetConfluentConfig(), out var producer))
                return;

            producer.Flush(ownerProducer.Endpoint.Configuration.FlushTimeout);
            producer.Dispose();
        }

        private IProducer<byte[]?, byte[]?> CreateConfluentProducer(KafkaProducer ownerProducer)
        {
            _logger.LogCreatingConfluentProducer(ownerProducer);

            var builder = _serviceProvider.GetRequiredService<IConfluentProducerBuilder>();
            builder.SetConfig(ownerProducer.Endpoint.Configuration.GetConfluentConfig());
            builder.SetEventsHandlers(ownerProducer, _callbacksInvoker, _logger);
            return builder.Build();
        }
    }
}
