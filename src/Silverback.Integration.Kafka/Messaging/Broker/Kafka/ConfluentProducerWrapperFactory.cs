// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal class ConfluentProducerWrapperFactory : IConfluentProducerWrapperFactory
{
    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly IServiceProvider _serviceProvider;

    private readonly ISilverbackLoggerFactory _silverbackLoggerFactory;

    public ConfluentProducerWrapperFactory(
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        IServiceProvider serviceProvider,
        ISilverbackLoggerFactory silverbackLoggerFactory)
    {
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _silverbackLoggerFactory = Check.NotNull(silverbackLoggerFactory, nameof(silverbackLoggerFactory));
    }

    public IConfluentProducerWrapper Create(string name, KafkaProducerConfiguration configuration)
    {
        // Ensure that a new instance is created for each client, since the confluent client builder cannot be reused
        IConfluentProducerBuilder confluentProducerBuilder = _serviceProvider.GetRequiredService<IConfluentProducerBuilder>();

        return new ConfluentProducerWrapper(
            name,
            confluentProducerBuilder,
            configuration,
            _brokerClientCallbacksInvoker,
            _silverbackLoggerFactory.CreateLogger<ConfluentProducerWrapper>());
    }
}
