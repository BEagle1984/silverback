﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class JsonSerializationFixture : KafkaFixture
{
    public JsonSerializationFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingDefaultSerializer()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].Should().BeEquivalentTo(typeof(TestEventOne).AssemblyQualifiedName);
        Helper.Spy.OutboundEnvelopes[1].Headers[DefaultMessageHeaders.MessageType].Should().BeEquivalentTo(typeof(TestEventTwo).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().Should().Be("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Hello E2E!");
        Helper.Spy.InboundEnvelopes[1].GetRawMessageAsString().Should().Be("{\"ContentEventTwo\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.InboundEnvelopes[1].Message.As<TestEventTwo>().ContentEventTwo.Should().Be("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingDefaultSerializerWithHardcodedType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].Should().BeEquivalentTo(typeof(TestEventOne).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().Should().Be("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldConsumeRegardlessOfTypeHeader_WhenUsingDefaultSerializerWithHardcodedType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().Should().Be("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingNewtonsoftSerializer()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).SerializeAsJsonUsingNewtonsoft()))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].Should().BeEquivalentTo(typeof(TestEventOne).AssemblyQualifiedName);
        Helper.Spy.OutboundEnvelopes[1].Headers[DefaultMessageHeaders.MessageType].Should().BeEquivalentTo(typeof(TestEventTwo).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().Should().Be("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Hello E2E!");
        Helper.Spy.InboundEnvelopes[1].GetRawMessageAsString().Should().Be("{\"ContentEventTwo\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.InboundEnvelopes[1].Message.As<TestEventTwo>().ContentEventTwo.Should().Be("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldProduceAndConsume_WhenUsingNewtonsoftSerializerWithHardcodedType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName).SerializeAsJsonUsingNewtonsoft()))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Headers[DefaultMessageHeaders.MessageType].Should().BeEquivalentTo(typeof(TestEventOne).AssemblyQualifiedName);

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().Should().Be("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Hello E2E!");
    }

    [Fact]
    public async Task JsonSerialization_ShouldConsumeRegardlessOfTypeHeader_WhenUsingNewtonsoftSerializerWithHardcodedType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].GetRawMessageAsString().Should().Be("{\"ContentEventOne\":\"Hello E2E!\"}");
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("Hello E2E!");
    }
}