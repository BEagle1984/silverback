// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ThrowIfUnhandledTests : E2ETestFixture
    {
        public ThrowIfUnhandledTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task ThrowIfUnhandled_DefaultSettings_ConsumerStoppedWhenUnhandled()
        {
            var receivedMessages = new List<object>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber((TestEventOne message) => { receivedMessages.Add(message); })
                        .AddDelegateSubscriber(
                            (IEnumerable<TestEventTwo> messages) =>
                            {
                                foreach (var message in messages)
                                {
                                    receivedMessages.Add(message);
                                }
                            })
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventThree> stream) =>
                            {
                                await foreach (var message in stream)
                                {
                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            var consumer = Host.ScopedServiceProvider.GetRequiredService<IBroker>().Consumers[0];

            SpyBehavior.InboundEnvelopes.Should().HaveCount(3);
            receivedMessages.Should().HaveCount(3);
            consumer.IsConnected.Should().BeTrue();

            await publisher.PublishAsync(new TestEventFour());
            await AsyncTestingUtil.WaitAsync(() => !consumer.IsConnected);

            SpyBehavior.InboundEnvelopes.Should().HaveCount(4);
            receivedMessages.Should().HaveCount(3);
            consumer.IsConnected.Should().BeFalse();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task ThrowIfUnhandled_ExceptionDisabled_MessageIgnoredWhenUnhandled()
        {
            var receivedMessages = new List<object>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ThrowIfUnhandled = false
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber((TestEventOne message) => { receivedMessages.Add(message); })
                        .AddDelegateSubscriber(
                            (IEnumerable<TestEventTwo> messages) =>
                            {
                                foreach (var message in messages)
                                {
                                    receivedMessages.Add(message);
                                }
                            })
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventThree> stream) =>
                            {
                                await foreach (var message in stream)
                                {
                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            var consumer = Host.ScopedServiceProvider.GetRequiredService<IBroker>().Consumers[0];

            SpyBehavior.InboundEnvelopes.Should().HaveCount(3);
            receivedMessages.Should().HaveCount(3);
            consumer.IsConnected.Should().BeTrue();

            await publisher.PublishAsync(new TestEventFour());
            await publisher.PublishAsync(new TestEventThree());
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(5);
            receivedMessages.Should().HaveCount(4);
            consumer.IsConnected.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);
        }

        [Fact]
        public async Task ThrowIfUnhandled_Batch_ConsumerStoppedWhenUnhandled()
        {
            var receivedMessages = new List<object>();
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration =
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Batch = new BatchSettings
                                        {
                                            Size = 3
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IEnumerable<TestEventOne> messages) =>
                            {
                                foreach (var message in messages)
                                {
                                    receivedMessages.Add(message);
                                }
                            })
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventTwo> stream) =>
                            {
                                await foreach (var message in stream)
                                {
                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventOne());
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            var consumer = Host.ScopedServiceProvider.GetRequiredService<IBroker>().Consumers[0];

            receivedMessages.Should().HaveCount(3);
            consumer.IsConnected.Should().BeTrue();

            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestEventThree());
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            await AsyncTestingUtil.WaitAsync(() => !consumer.IsConnected);

            receivedMessages.Should().HaveCount(4);
            consumer.IsConnected.Should().BeFalse();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }
    }
}
