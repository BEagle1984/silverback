// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientsConfigurationBuilderFixture
{
    [Fact]
    public async Task WithBootstrapServers_ShouldSetBootstrapServersForAllClients()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://unittest")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("consumer1")
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        consumers.Count.ShouldBe(1);
        KafkaConsumer kafkaConsumer = consumers[0].ShouldBeOfType<KafkaConsumer>();
        kafkaConsumer.Configuration.BootstrapServers.ShouldBe("PLAINTEXT://unittest");

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Count.ShouldBe(1);
        KafkaProducer kafkaProducer = producers[0].ShouldBeOfType<KafkaProducer>();
        kafkaProducer.Configuration.BootstrapServers.ShouldBe("PLAINTEXT://unittest");
    }

    [Fact]
    public async Task AddProducer_ShouldAddProducers()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://unittest")
                        .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1")))
                        .AddProducer(producer => producer.Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Count.ShouldBe(2);
        KafkaProducerEndpointConfiguration endpointConfiguration1 = producers[0].EndpointConfiguration.ShouldBeOfType<KafkaProducerEndpointConfiguration>();
        endpointConfiguration1.MessageType.ShouldBe(typeof(TestEventOne));
        endpointConfiguration1.EndpointResolver.RawName.ShouldBe("topic1");
        KafkaProducerEndpointConfiguration endpointConfiguration2 = producers[1].EndpointConfiguration.ShouldBeOfType<KafkaProducerEndpointConfiguration>();
        endpointConfiguration2.MessageType.ShouldBe(typeof(TestEventTwo));
        endpointConfiguration2.EndpointResolver.RawName.ShouldBe("topic2");
    }

    [Fact]
    public async Task AddProducer_ShouldAddTransactionalProducers()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://unittest")
                        .AddProducer(producer => producer.Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1")))
                        .AddProducer(
                            producer => producer
                                .EnableTransactions("whatever")
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Count.ShouldBe(2);
        producers[0].ShouldBeOfType<KafkaProducer>();
        producers[0].EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventOne));
        KafkaProducerEndpointConfiguration endpointConfiguration1 = producers[0].EndpointConfiguration.ShouldBeOfType<KafkaProducerEndpointConfiguration>();
        endpointConfiguration1.EndpointResolver.RawName.ShouldBe("topic1");
        producers[1].ShouldBeOfType<KafkaTransactionalProducer>();
        producers[1].EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventTwo));
        KafkaProducerEndpointConfiguration endpointConfiguration2 = producers[1].EndpointConfiguration.ShouldBeOfType<KafkaProducerEndpointConfiguration>();
        endpointConfiguration2.EndpointResolver.RawName.ShouldBe("topic2");
    }

    [Fact]
    public async Task AddProducer_ShouldMergeProducerConfiguration_WhenIdIsTheSame()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://unittest")
                        .AddProducer(
                            "producer1",
                            producer => producer
                                .WithBatchSize(42)
                                .WithLingerMs(1)
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1")))
                        .AddProducer(
                            "producer1",
                            producer => producer
                                .WithLingerMs(42)
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        KafkaProducer[] producers = serviceProvider.GetRequiredService<ProducerCollection>().Cast<KafkaProducer>().ToArray();
        producers.Length.ShouldBe(2);
        producers[0].EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventOne));
        KafkaProducerEndpointConfiguration endpointConfiguration1 = producers[0].EndpointConfiguration.ShouldBeOfType<KafkaProducerEndpointConfiguration>();
        endpointConfiguration1.EndpointResolver.RawName.ShouldBe("topic1");
        producers[0].Configuration.BatchSize.ShouldBe(42);
        producers[0].Configuration.LingerMs.ShouldBe(42);
        producers[1].EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventTwo));
        KafkaProducerEndpointConfiguration endpointConfiguration2 = producers[1].EndpointConfiguration.ShouldBeOfType<KafkaProducerEndpointConfiguration>();
        endpointConfiguration2.EndpointResolver.RawName.ShouldBe("topic2");
        producers[1].Configuration.BatchSize.ShouldBe(42);
        producers[1].Configuration.LingerMs.ShouldBe(42);
        producers[1].Client.ShouldBeSameAs(producers[0].Client);
    }

    [Fact]
    public async Task AddConsumer_ShouldAddConsumers()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://unittest")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("consumer1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("consumer2")
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        KafkaConsumer[] consumers = serviceProvider.GetRequiredService<ConsumerCollection>().Cast<KafkaConsumer>().ToArray();
        consumers.Length.ShouldBe(2);
        consumers[0].Configuration.Endpoints.First().Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        consumers[0].Configuration.Endpoints.First().RawName.ShouldBe("topic1");
        consumers[1].Configuration.Endpoints.First().Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventTwo>>();
        consumers[1].Configuration.Endpoints.First().RawName.ShouldBe("topic2");
    }

    [Fact]
    public async Task AddConsumer_ShouldMergeConsumerConfiguration_WhenIdIsTheSame()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://unittest")
                        .AddConsumer(
                            "consumer1",
                            consumer => consumer
                                .WithGroupId("group1")
                                .WithFetchMinBytes(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddConsumer(
                            "consumer1",
                            consumer => consumer
                                .WithEnablePartitionEof(true)
                                .WithFetchMinBytes(42)
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        KafkaConsumer[] consumers = serviceProvider.GetRequiredService<ConsumerCollection>().Cast<KafkaConsumer>().ToArray();
        consumers.Length.ShouldBe(1);
        consumers[0].Configuration.Endpoints.Count.ShouldBe(2);
        consumers[0].Configuration.Endpoints.First().TopicPartitions.Count.ShouldBe(1);
        consumers[0].Configuration.Endpoints.First().RawName.ShouldBe("topic1");
        consumers[0].Configuration.Endpoints.Skip(1).First().TopicPartitions.Count.ShouldBe(1);
        consumers[0].Configuration.Endpoints.Skip(1).First().RawName.ShouldBe("topic2");
        consumers[0].Configuration.GroupId.ShouldBe("group1");
        consumers[0].Configuration.EnablePartitionEof.ShouldBe(true);
        consumers[0].Configuration.FetchMinBytes.ShouldBe(42);
    }
}
