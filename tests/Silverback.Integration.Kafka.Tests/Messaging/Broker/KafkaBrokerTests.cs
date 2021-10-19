// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public sealed class KafkaBrokerTests : IDisposable
{
    private readonly KafkaBroker _broker;

    public KafkaBrokerTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka()));

        _broker = serviceProvider.GetRequiredService<KafkaBroker>();
    }

    [Fact]
    public void GetProducer_SomeEndpoint_ProducerReturned()
    {
        KafkaProducerConfiguration endpoint = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                })
            .Build();

        IProducer producer = _broker.GetProducer(endpoint);

        producer.Should().NotBeNull();
    }

    [Fact]
    public void GetProducer_SameEndpointInstance_SameInstanceReturned()
    {
        KafkaProducerConfiguration endpoint = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                })
            .Build();

        IProducer producer1 = _broker.GetProducer(endpoint);
        IProducer producer2 = _broker.GetProducer(endpoint);

        producer2.Should().BeSameAs(producer1);
    }

    [Fact]
    public void GetProducer_SameEndpointConfiguration_SameInstanceReturned()
    {
        KafkaProducerConfiguration endpoint1 = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.MessageTimeoutMs = 2000;
                })
            .Build();
        KafkaProducerConfiguration endpoint2 = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.MessageTimeoutMs = 2000;
                })
            .Build();

        IProducer producer1 = _broker.GetProducer(endpoint1);
        IProducer producer2 = _broker.GetProducer(endpoint2);

        producer2.Should().BeSameAs(producer1);
    }

    [Fact]
    public void GetProducer_DifferentEndpoint_DifferentInstanceReturned()
    {
        KafkaProducerConfiguration endpoint1 = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                })
            .Build();
        KafkaProducerConfiguration endpoint2 = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("other-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                })
            .Build();

        IProducer producer1 = _broker.GetProducer(endpoint1);
        IProducer producer2 = _broker.GetProducer(endpoint2);

        producer2.Should().NotBeSameAs(producer1);
    }

    [Fact]
    public void GetProducer_DifferentEndpointConfiguration_DifferentInstanceReturned()
    {
        KafkaProducerConfiguration endpoint1 = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.MessageTimeoutMs = 2000;
                })
            .Build();
        KafkaProducerConfiguration endpoint2 = new KafkaProducerConfigurationBuilder<object>()
            .ProduceTo("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.MessageTimeoutMs = 9999;
                })
            .Build();

        IProducer producer1 = _broker.GetProducer(endpoint1);
        IProducer producer2 = _broker.GetProducer(endpoint2);

        producer2.Should().NotBeSameAs(producer1);
    }

    [Fact]
    public void AddConsumer_SomeEndpoint_ConsumerReturned()
    {
        KafkaConsumerConfiguration endpoint = new KafkaConsumerConfigurationBuilder<object>()
            .ConsumeFrom("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.GroupId = "group1";
                })
            .Build();

        IConsumer consumer = _broker.AddConsumer(endpoint);

        consumer.Should().NotBeNull();
    }

    [Fact]
    public void AddConsumer_SameEndpointInstance_DifferentInstanceReturned()
    {
        KafkaConsumerConfiguration endpoint = new KafkaConsumerConfigurationBuilder<object>()
            .ConsumeFrom("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.GroupId = "group1";
                })
            .Build();

        IConsumer consumer1 = _broker.AddConsumer(endpoint);
        IConsumer consumer2 = _broker.AddConsumer(endpoint);

        consumer2.Should().NotBeSameAs(consumer1);
    }

    [Fact]
    public void AddConsumer_SameEndpointConfiguration_DifferentInstanceReturned()
    {
        KafkaConsumerConfiguration endpoint1 = new KafkaConsumerConfigurationBuilder<object>()
            .ConsumeFrom("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.GroupId = "group1";
                })
            .Build();
        KafkaConsumerConfiguration endpoint2 = new KafkaConsumerConfigurationBuilder<object>()
            .ConsumeFrom("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.GroupId = "group1";
                })
            .Build();

        IConsumer consumer1 = _broker.AddConsumer(endpoint1);
        IConsumer consumer2 = _broker.AddConsumer(endpoint2);

        consumer2.Should().NotBeSameAs(consumer1);
    }

    [Fact]
    public void AddConsumer_DifferentEndpoint_DifferentInstanceReturned()
    {
        KafkaConsumerConfiguration endpoint1 = new KafkaConsumerConfigurationBuilder<object>()
            .ConsumeFrom("test-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.GroupId = "group1";
                })
            .Build();
        KafkaConsumerConfiguration endpoint2 = new KafkaConsumerConfigurationBuilder<object>()
            .ConsumeFrom("other-endpoint")
            .ConfigureClient(
                config =>
                {
                    config.BootstrapServers = "PLAINTEXT://whatever:1111";
                    config.GroupId = "group1";
                })
            .Build();

        IConsumer consumer1 = _broker.AddConsumer(endpoint1);
        IConsumer consumer2 = _broker.AddConsumer(endpoint2);

        consumer2.Should().NotBeSameAs(consumer1);
    }

    public void Dispose() => _broker.Dispose();
}
