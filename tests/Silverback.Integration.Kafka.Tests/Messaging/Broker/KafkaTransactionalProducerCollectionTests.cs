// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Transactions;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public sealed class KafkaTransactionalProducerCollectionTests : IDisposable
{
    private readonly KafkaProducerConfiguration _configuration;

    private readonly IConfluentProducerWrapperFactory _factory = Substitute.For<IConfluentProducerWrapperFactory>();

    private readonly List<IConfluentProducerWrapper> _createdClients = [];

    private readonly BrokerClientCollection _clients = [];

    private readonly KafkaTransactionalProducerCollection _collection;

    public KafkaTransactionalProducerCollectionTests()
    {
        _configuration = new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .EnableTransactions("transactional1")
            .Produce<object>(endpoint => endpoint.ProduceTo("topic1"))
            .Build();

        _factory.Create(Arg.Any<string>(), Arg.Any<KafkaProducerConfiguration>()).Returns(call =>
        {
            IConfluentProducerWrapper client = Substitute.For<IConfluentProducerWrapper>();
            client.Name.Returns(call.ArgAt<string>(0));
            client.Configuration.Returns(call.ArgAt<KafkaProducerConfiguration>(1));
            _createdClients.Add(client);

            return client;
        });

        IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
        behaviors.GetBehaviorsList().Returns([]);

        _collection = new KafkaTransactionalProducerCollection(
            _factory,
            _clients,
            Substitute.For<ISilverbackLogger<KafkaProducer>>(),
            behaviors,
            new RootServiceProvider(Substitute.For<IServiceProvider>()));
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCreateConnectAndInitTransactionalProducer()
    {
        KafkaProducer producer = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("suffix"));

        producer.Name.ShouldBe("producer1|suffix");
        producer.Configuration.TransactionalId.ShouldBe("transactional1|suffix");
        producer.Configuration.Endpoints.ShouldBe(_configuration.Endpoints);
        IConfluentProducerWrapper client = _createdClients.ShouldHaveSingleItem();
        producer.Client.ShouldBeSameAs(client);
        _factory.Received(1).Create(
            "producer1|suffix",
            Arg.Is<KafkaProducerConfiguration>(configuration => configuration.TransactionalId == "transactional1|suffix"));
        await client.Received(1).ConnectAsync();
        client.Received(1).InitTransactions();
        _clients["producer1|suffix"].ShouldBeSameAs(client);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetOrCreateAsync_ShouldNotAppendSuffixToName_WhenTransactionHasNoSuffix(string? suffix)
    {
        KafkaProducer producer = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction(suffix));

        producer.Name.ShouldBe("producer1");
        producer.Configuration.TransactionalId.ShouldBe("transactional1|");
        _clients["producer1"].ShouldBeSameAs(producer.Client);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnSameProducer_WhenSuffixIsTheSame()
    {
        KafkaProducer producer1 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("suffix"));
        KafkaProducer producer2 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("suffix"));

        producer2.ShouldBeSameAs(producer1);
        IConfluentProducerWrapper client = _createdClients.ShouldHaveSingleItem();
        await client.Received(1).ConnectAsync();
        client.Received(1).InitTransactions();
        _collection.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCreateDistinctProducers_WhenSuffixesDiffer()
    {
        KafkaProducer producer1 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("a"));
        KafkaProducer producer2 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("b"));

        producer2.ShouldNotBeSameAs(producer1);
        producer1.Configuration.TransactionalId.ShouldBe("transactional1|a");
        producer2.Configuration.TransactionalId.ShouldBe("transactional1|b");
        _createdClients.Count.ShouldBe(2);
        _clients.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Count_ShouldReturnNumberOfCreatedProducers()
    {
        _collection.Count.ShouldBe(0);

        await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("a"));
        await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("b"));

        _collection.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetEnumerator_ShouldEnumerateCreatedProducers()
    {
        KafkaProducer producer1 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("a"));
        KafkaProducer producer2 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("b"));

        _collection.ShouldBe([producer1, producer2], ignoreOrder: true);
    }

    [Fact]
    public async Task GetEnumerator_ShouldEnumerateCreatedProducers_WhenEnumeratedAsNonGenericEnumerable()
    {
        KafkaProducer producer1 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("a"));
        KafkaProducer producer2 = await _collection.GetOrCreateAsync("producer1", _configuration, CreateEnvelope(), CreateTransaction("b"));

        List<object> enumerated = [];
        foreach (object producer in (IEnumerable)_collection)
        {
            enumerated.Add(producer);
        }

        enumerated.ShouldBe([producer1, producer2], ignoreOrder: true);
    }

    public void Dispose()
    {
        foreach (KafkaProducer producer in _collection)
        {
            producer.Dispose();
        }

        _clients.Dispose();
    }

    private static IOutboundEnvelope CreateEnvelope() => Substitute.For<IOutboundEnvelope>();

    private static IKafkaTransaction CreateTransaction(string? suffix)
    {
        IKafkaTransaction transaction = Substitute.For<IKafkaTransaction>();
        transaction.TransactionalIdSuffix.Returns(suffix);

        return transaction;
    }
}
