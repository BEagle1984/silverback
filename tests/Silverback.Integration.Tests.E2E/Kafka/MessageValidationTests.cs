// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Validation;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class MessageValidationTests : KafkaTestFixture
    {
        public MessageValidationTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Validation_ThrowException_InvalidMessageNotProduced()
        {
            var message = new TestValidationMessage
            {
                String10 = "1234567890abcd"
            };
            var expectedMessage =
                $"The message is not valid:{Environment.NewLine}- The field String10 must be a string with a maximum length of 10.";

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ValidateMessage(true)
                                        .ProduceTo(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            Func<Task> act = () => publisher.PublishAsync(message);

            await act.Should().ThrowAsync<MessageValidationException>().WithMessage(expectedMessage);
            DefaultTopic.MessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task Validation_None_InvalidMessageIsProduced()
        {
            var message = new TestValidationMessage
            {
                String10 = "1234567890abcd"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .DisableMessageValidation()
                                        .ProduceTo(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            Func<Task> act = () => publisher.PublishAsync(message);

            await act.Should().NotThrowAsync<ValidationException>();
            DefaultTopic.MessagesCount.Should().Be(1);
        }

        [Fact]
        public async Task Validation_WithWarning_InvalidMessageIsProduced()
        {
            var message = new TestValidationMessage
            {
                String10 = "1234567890abcd"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ValidateMessage(false)
                                        .ProduceTo(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            Func<Task> act = () => publisher.PublishAsync(message);

            await act.Should().NotThrowAsync<ValidationException>();
            DefaultTopic.MessagesCount.Should().Be(1);
        }

        [Fact]
        public async Task Validation_ThrowException_InvalidMessageNotConsumed()
        {
            bool received = false;

            Host.ConfigureServices(
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .Configure(
                                        config =>
                                        {
                                            config.BootstrapServers = "PLAINTEXT://tests";
                                        })
                                    .AddOutbound<IIntegrationEvent>(
                                        endpoint => endpoint.ProduceTo(DefaultTopicName))
                                    .AddInbound<TestValidationMessage>(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .ValidateMessage(true)
                                            .Configure(
                                                config =>
                                                {
                                                    config.GroupId = DefaultConsumerGroupId;
                                                })))
                            .AddDelegateSubscriber(
                                (IInboundEnvelope _) =>
                                {
                                    received = true;
                                })
                            .AddIntegrationSpyAndSubscriber();
                    })
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(Encoding.UTF8.GetBytes("{\"String10\": \"1234567890abcd\"}"));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
            received.Should().BeFalse();

            await AsyncTestingUtil.WaitAsync(() => Helper.Broker.Consumers[0].IsConnected == false);
            Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Validation_None_InvalidMessageConsumed()
        {
            bool received = false;

            Host.ConfigureServices(
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .Configure(
                                        config =>
                                        {
                                            config.BootstrapServers = "PLAINTEXT://tests";
                                        })
                                    .AddOutbound<IIntegrationEvent>(
                                        endpoint => endpoint.ProduceTo(DefaultTopicName))
                                    .AddInbound<TestValidationMessage>(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .DisableMessageValidation()
                                            .Configure(
                                                config =>
                                                {
                                                    config.GroupId = DefaultConsumerGroupId;
                                                })))
                            .AddDelegateSubscriber(
                                (IInboundEnvelope _) =>
                                {
                                    received = true;
                                })
                            .AddIntegrationSpyAndSubscriber();
                    })
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(Encoding.UTF8.GetBytes("{\"String10\": \"1234567890abcd\"}"));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
            received.Should().BeTrue();
            Helper.Broker.Consumers[0].IsConnected.Should().BeTrue();
        }

        [Fact]
        public async Task Validation_WithWarning_InvalidMessageConsumed()
        {
            bool received = false;

            Host.ConfigureServices(
                    services =>
                    {
                        services
                            .AddLogging()
                            .AddSilverback()
                            .UseModel()
                            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                            .AddKafkaEndpoints(
                                endpoints => endpoints
                                    .Configure(
                                        config =>
                                        {
                                            config.BootstrapServers = "PLAINTEXT://tests";
                                        })
                                    .AddOutbound<IIntegrationEvent>(
                                        endpoint => endpoint.ProduceTo(DefaultTopicName))
                                    .AddInbound<TestValidationMessage>(
                                        endpoint => endpoint
                                            .ConsumeFrom(DefaultTopicName)
                                            .ValidateMessage(false)
                                            .Configure(
                                                config =>
                                                {
                                                    config.GroupId = DefaultConsumerGroupId;
                                                })))
                            .AddDelegateSubscriber(
                                (IInboundEnvelope _) =>
                                {
                                    received = true;
                                })
                            .AddIntegrationSpyAndSubscriber();
                    })
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);

            await producer.ProduceAsync(Encoding.UTF8.GetBytes("{\"String10\": \"1234567890abcd\"}"));

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
            received.Should().BeTrue();
            Helper.Broker.Consumers[0].IsConnected.Should().BeTrue();
        }
    }
}
