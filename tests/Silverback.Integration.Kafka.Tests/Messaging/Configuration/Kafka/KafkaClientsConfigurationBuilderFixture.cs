// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        consumers.Should().HaveCount(1);
        consumers[0].As<KafkaConsumer>().Configuration.BootstrapServers.Should().Be("PLAINTEXT://unittest");

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Should().HaveCount(1);
        producers[0].As<KafkaProducer>().Configuration.BootstrapServers.Should().Be("PLAINTEXT://unittest");
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
        producers.Should().HaveCount(2);
        producers[0].EndpointConfiguration.MessageType.Should().Be<TestEventOne>();
        producers[0].EndpointConfiguration.As<KafkaProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic1");
        producers[1].EndpointConfiguration.MessageType.Should().Be<TestEventTwo>();
        producers[1].EndpointConfiguration.As<KafkaProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic2");
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
        producers.Should().HaveCount(2);
        producers[0].Should().BeOfType<KafkaProducer>();
        producers[0].EndpointConfiguration.MessageType.Should().Be<TestEventOne>();
        producers[0].EndpointConfiguration.As<KafkaProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic1");
        producers[1].Should().BeOfType<KafkaTransactionalProducer>();
        producers[1].EndpointConfiguration.MessageType.Should().Be<TestEventTwo>();
        producers[1].EndpointConfiguration.As<KafkaProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic2");
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
        producers.Should().HaveCount(2);
        producers[0].EndpointConfiguration.MessageType.Should().Be<TestEventOne>();
        producers[0].EndpointConfiguration.As<KafkaProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic1");
        producers[0].Configuration.BatchSize.Should().Be(42);
        producers[0].Configuration.LingerMs.Should().Be(42);
        producers[1].EndpointConfiguration.MessageType.Should().Be<TestEventTwo>();
        producers[1].EndpointConfiguration.As<KafkaProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic2");
        producers[1].Configuration.BatchSize.Should().Be(42);
        producers[1].Configuration.LingerMs.Should().Be(42);
        producers[1].Client.Should().BeSameAs(producers[0].Client);
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
        consumers.Should().HaveCount(2);
        consumers[0].Configuration.Endpoints.First().Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
        consumers[0].Configuration.Endpoints.First().RawName.Should().Be("topic1");
        consumers[1].Configuration.Endpoints.First().Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventTwo>>();
        consumers[1].Configuration.Endpoints.First().RawName.Should().Be("topic2");
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
        consumers.Should().HaveCount(1);
        consumers[0].Configuration.Endpoints.Should().HaveCount(2);
        consumers[0].Configuration.Endpoints.First().TopicPartitions.Should().HaveCount(1);
        consumers[0].Configuration.Endpoints.First().RawName.Should().Be("topic1");
        consumers[0].Configuration.Endpoints.Skip(1).First().TopicPartitions.Should().HaveCount(1);
        consumers[0].Configuration.Endpoints.Skip(1).First().RawName.Should().Be("topic2");
        consumers[0].Configuration.GroupId.Should().Be("group1");
        consumers[0].Configuration.EnablePartitionEof.Should().BeTrue();
        consumers[0].Configuration.FetchMinBytes.Should().Be(42);
    }
}
