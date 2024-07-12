// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Validation;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class MessageValidationFixture : KafkaFixture
{
    public MessageValidationFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Validation_ShouldNotProduceInvalidMessage_WhenValidationEnabled()
    {
        TestValidationMessage message = new() { String10 = "1234567890abcd" };
        string expectedMessage = $"The message is not valid: {Environment.NewLine}" +
                                 "- The field String10 must be a string with a maximum length of 10.";

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .ValidateMessageAndThrow()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishEventAsync(message).AsTask();

        await act.Should().ThrowAsync<MessageValidationException>().WithMessage(expectedMessage);

        Host.ServiceProvider.GetRequiredService<IInMemoryTopicCollection>().Should().BeEmpty(); // the topic is created when the first message is produced
    }

    [Fact]
    public async Task Validation_ShouldProduceInvalidMessage_WhenValidationDisabled()
    {
        TestValidationMessage message = new() { String10 = "1234567890abcd" };

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .DisableMessageValidation()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishEventAsync(message).AsTask();

        await act.Should().NotThrowAsync<ValidationException>();
        DefaultTopic.MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task Validation_ShouldProduceInvalidMessage_WhenValidationModeWarning()
    {
        TestValidationMessage message = new() { String10 = "1234567890abcd" };

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .ValidateMessageAndWarn()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishEventAsync(message).AsTask();

        await act.Should().NotThrowAsync<ValidationException>();
        DefaultTopic.MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task Validation_ShouldNotConsumeInvalidMessage_WhenValidationEnabled()
    {
        bool received = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestValidationMessage>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ValidateMessageAndThrow())))
                .AddDelegateSubscriber<TestValidationMessage>(HandleMessage)
                .AddIntegrationSpyAndSubscriber());

        void HandleMessage(TestValidationMessage dummy) => received = true;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(Encoding.UTF8.GetBytes("{\"String10\": \"1234567890abcd\"}"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        received.Should().BeFalse();

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
        consumer.Client.Status.Should().Be(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Validation_ShouldConsumeInvalidMessage_WhenValidationDisabled()
    {
        bool received = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestValidationMessage>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DisableMessageValidation())))
                .AddDelegateSubscriber<TestValidationMessage>(HandleMessage)
                .AddIntegrationSpyAndSubscriber());

        void HandleMessage(TestValidationMessage dummy) => received = true;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(Encoding.UTF8.GetBytes("{\"String10\": \"1234567890abcd\"}"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
        received.Should().BeTrue();

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);
        consumer.Client.Status.Should().Be(ClientStatus.Initialized);
    }

    [Fact]
    public async Task Validation_ShouldConsumeInvalidMessage_WhenValidationModeWarning()
    {
        bool received = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestValidationMessage>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .ValidateMessageAndWarn())))
                .AddDelegateSubscriber<TestValidationMessage>(HandleMessage)
                .AddIntegrationSpyAndSubscriber());

        void HandleMessage(TestValidationMessage dummy) => received = true;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(Encoding.UTF8.GetBytes("{\"String10\": \"1234567890abcd\"}"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
        received.Should().BeTrue();

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);
        consumer.Client.Status.Should().Be(ClientStatus.Initialized);
    }
}
