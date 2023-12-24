// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

internal class KafkaProducersInitializer : BrokerClientsInitializer
{
    private readonly KafkaClientsConfigurationActions _configurationActions;

    private readonly IProducerLogger<KafkaProducer> _producerLogger;

    private readonly IBrokerBehaviorsProvider<IProducerBehavior> _behaviorsProvider;

    private readonly IOutboundEnvelopeFactory _envelopeFactory;

    private readonly IConfluentProducerWrapperFactory _confluentProducerWrapperFactory;

    private readonly IKafkaTransactionalProducerCollection _transactionalProducers;

    public KafkaProducersInitializer(
        KafkaClientsConfigurationActions configurationActions,
        IProducerLogger<KafkaProducer> producerLogger,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IConfluentProducerWrapperFactory confluentProducerWrapperFactory,
        IKafkaTransactionalProducerCollection transactionalProducers,
        IServiceProvider serviceProvider,
        ISilverbackLogger<KafkaProducersInitializer> logger)
        : base(serviceProvider, logger)
    {
        _configurationActions = Check.NotNull(configurationActions, nameof(configurationActions));
        _producerLogger = Check.NotNull(producerLogger, nameof(producerLogger));
        _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
        _envelopeFactory = Check.NotNull(envelopeFactory, nameof(envelopeFactory));
        _confluentProducerWrapperFactory = Check.NotNull(confluentProducerWrapperFactory, nameof(confluentProducerWrapperFactory));
        _transactionalProducers = Check.NotNull(transactionalProducers, nameof(transactionalProducers));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifecycle handled by the ProducerCollection")]
    internal IReadOnlyCollection<IProducer> InitializeProducers(string name, KafkaProducerConfiguration configuration, bool routing = true) =>
        configuration.IsTransactional
            ? InitializeTransactionalProducers(name, configuration, routing)
            : InitializeNormalProducers(name, configuration, routing);

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

    private IReadOnlyCollection<IProducer> InitializeNormalProducers(string name, KafkaProducerConfiguration configuration, bool routing)
    {
        // TODO: Append suffix to name?
        IConfluentProducerWrapper confluentProducerWrapper = _confluentProducerWrapperFactory.Create(name, configuration);
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

    private IReadOnlyCollection<IProducer> InitializeTransactionalProducers(string name, KafkaProducerConfiguration configuration, bool routing)
    {
        int i = 0;
        List<KafkaTransactionalProducer> producers = new();

        foreach (KafkaProducerEndpointConfiguration endpointConfiguration in configuration.Endpoints)
        {
            KafkaProducerConfiguration trimmedConfiguration = configuration with
            {
                Endpoints = new[] { endpointConfiguration }.AsValueReadOnlyCollection()
            };

            KafkaTransactionalProducer producer = new(
                configuration.Endpoints.Count > 0 ? $"{name}-{++i}" : name,
                trimmedConfiguration,
                _transactionalProducers);

            AddProducer(producer, routing);
            producers.Add(producer);
        }

        return producers;
    }
}
