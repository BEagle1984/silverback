// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

internal class KafkaProducersInitializer : BrokerClientsInitializer
{
    private readonly KafkaClientsConfigurationActions _configurationActions;

    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly ISilverbackLoggerFactory _silverbackLoggerFactory;

    private readonly IProducerLogger<KafkaProducer> _producerLogger;

    private readonly IBrokerBehaviorsProvider<IProducerBehavior> _behaviorsProvider;

    private readonly IOutboundEnvelopeFactory _envelopeFactory;

    public KafkaProducersInitializer(
        KafkaClientsConfigurationActions configurationActions,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        ISilverbackLoggerFactory silverbackLoggerFactory,
        IProducerLogger<KafkaProducer> producerLogger,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        ISilverbackLogger<KafkaProducersInitializer> logger)
        : base(serviceProvider, logger)
    {
        _configurationActions = Check.NotNull(configurationActions, nameof(configurationActions));
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _silverbackLoggerFactory = Check.NotNull(silverbackLoggerFactory, nameof(silverbackLoggerFactory));
        _producerLogger = Check.NotNull(producerLogger, nameof(producerLogger));
        _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
        _envelopeFactory = Check.NotNull(envelopeFactory, nameof(envelopeFactory));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifecycle handled by the ProducerCollection")]
    internal IReadOnlyCollection<KafkaProducer> InitializeProducers(string name, KafkaProducerConfiguration configuration, bool routing = true)
    {
        ConfluentProducerWrapper confluentProducerWrapper = new(
            name, // TODO: Add suffix?
            ServiceProvider.GetRequiredService<IConfluentProducerBuilder>(),
            configuration,
            _brokerClientCallbacksInvoker,
            _silverbackLoggerFactory.CreateLogger<ConfluentProducerWrapper>());

        AddClient(confluentProducerWrapper);

        int i = 0;
        List<KafkaProducer> producers = new();

        foreach (KafkaProducerEndpointConfiguration endpointConfiguration in configuration.Endpoints)
        {
            KafkaProducerConfiguration trimmedConfiguration = configuration with
            {
                Endpoints = new[] { endpointConfiguration }.AsValueReadOnlyCollection()
            };

            KafkaProducer producer = new(
                configuration.Endpoints.Count > 0 ? $"{name}-{++i}" : name,
                confluentProducerWrapper,
                trimmedConfiguration,
                _behaviorsProvider,
                _envelopeFactory,
                ServiceProvider,
                _producerLogger);

            AddProducer(producer, routing);

            producers.Add(producer);
        }

        return producers;
    }

    protected override void InitializeCore()
    {
        foreach (MergedAction<KafkaProducerConfigurationBuilder>? mergedAction in _configurationActions.ProducerConfigurationActions)
        {
            KafkaProducerConfigurationBuilder builder = new();
            mergedAction.Action.Invoke(builder);
            KafkaProducerConfiguration configuration = builder.Build();

            InitializeProducers(mergedAction.Key, configuration);
        }
    }
}
