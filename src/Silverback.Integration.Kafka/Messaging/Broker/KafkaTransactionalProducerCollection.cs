// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Transactions;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal class KafkaTransactionalProducerCollection : IKafkaTransactionalProducerCollection
{
    private readonly IConfluentProducerWrapperFactory _factory;

    private readonly BrokerClientCollection _clients;

    private readonly ProducerCollection _producers;

    private readonly IProducerLogger<KafkaProducer> _producerLogger;

    private readonly IBrokerBehaviorsProvider<IProducerBehavior> _behaviorsProvider;

    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<string, KafkaProducer> _producersByName = new();

    public KafkaTransactionalProducerCollection(
        IConfluentProducerWrapperFactory factory,
        BrokerClientCollection clients,
        ProducerCollection producers,
        IProducerLogger<KafkaProducer> producerLogger,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        RootServiceProvider rootServiceProvider)
    {
        _factory = Check.NotNull(factory, nameof(factory));
        _clients = Check.NotNull(clients, nameof(clients));
        _producers = Check.NotNull(producers, nameof(producers));
        _producerLogger = Check.NotNull(producerLogger, nameof(producerLogger));
        _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));

        // Ensure that the root service provider is used to resolve the needed service, to avoid premature disposal
        _serviceProvider = rootServiceProvider.ServiceProvider;
    }

    public int Count => _producersByName.Count;

    public async ValueTask<KafkaProducer> GetOrCreateAsync(
        string name,
        KafkaProducerConfiguration configuration,
        IOutboundEnvelope envelope,
        IKafkaTransaction transaction)
    {
        if (!string.IsNullOrEmpty(transaction.TransactionalIdSuffix))
            name = $"{name}|{transaction.TransactionalIdSuffix}";

        if (!_producersByName.TryGetValue(name, out KafkaProducer? producer))
        {
            FactoryArguments factoryArgument = new(
                _factory,
                _clients,
                _producers,
                configuration with
                {
                    TransactionalId = $"{configuration.TransactionalId}|{transaction.TransactionalIdSuffix}"
                },
                _behaviorsProvider,
                _serviceProvider,
                _producerLogger);

            producer = _producersByName.GetOrAdd(
                name,
                static (nameKey, args) =>
                {
                    IConfluentProducerWrapper client = args.ClientFactory.Create(nameKey, args.Configuration);
                    args.Clients.Add(client);

                    KafkaProducer producer = new(
                        nameKey,
                        client,
                        args.Configuration,
                        args.BehaviorsProvider,
                        args.ServiceProvider,
                        args.ProducerLogger);
                    args.Producers.Add(producer, false);

                    return producer;
                },
                factoryArgument);

            await producer.Client.ConnectAsync().ConfigureAwait(false);
            producer.Client.InitTransactions();
        }

        return producer;
    }

    [MustDisposeResource]
    public IEnumerator<KafkaProducer> GetEnumerator() => _producersByName.Values.GetEnumerator();

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private record struct FactoryArguments(
        IConfluentProducerWrapperFactory ClientFactory,
        BrokerClientCollection Clients,
        ProducerCollection Producers,
        KafkaProducerConfiguration Configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> BehaviorsProvider,
        IServiceProvider ServiceProvider,
        IProducerLogger<KafkaProducer> ProducerLogger);
}
