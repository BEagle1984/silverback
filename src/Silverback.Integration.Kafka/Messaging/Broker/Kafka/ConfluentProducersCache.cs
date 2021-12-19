// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

internal sealed class ConfluentProducersCache : IConfluentProducersCache
{
    private readonly ConcurrentDictionary<KafkaClientProducerConfiguration, IProducer<byte[]?, byte[]?>> _producersCache = new();

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

    public IProducer<byte[]?, byte[]?> GetProducer(KafkaClientProducerConfiguration configuration, KafkaProducer owner) =>
        _producersCache.GetOrAdd(
            configuration,
            (keyConfiguration, ownerArgument) => CreateConfluentProducer(keyConfiguration, ownerArgument),
            owner);

    public void DisposeProducer(KafkaClientProducerConfiguration configuration)
    {
        // Dispose only if still in cache to avoid ObjectDisposedException
        if (!_producersCache.TryRemove(configuration, out IProducer<byte[]?, byte[]?>? producer))
            return;

        producer.Flush(configuration.FlushTimeout);
        producer.Dispose();
    }

    private IProducer<byte[]?, byte[]?> CreateConfluentProducer(
        KafkaClientProducerConfiguration configuration,
        KafkaProducer ownerProducer)
    {
        _logger.LogCreatingConfluentProducer(ownerProducer);

        IConfluentProducerBuilder? builder = _serviceProvider.GetRequiredService<IConfluentProducerBuilder>();
        builder.SetConfig(configuration.GetConfluentClientConfig());
        builder.SetEventsHandlers(ownerProducer, _callbacksInvoker, _logger);
        return builder.Build();
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "False positive, remove suppression once record struct is handled properly")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "False positive, remove suppression once record struct is handled properly")]
    private record struct NewProducerParametersNewProducerParameters(KafkaClientProducerConfiguration Configuration, KafkaProducer Owner);
}
