// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [Trait("Category", "E2E")]
    public class BasicTests : E2ETestFixture
    {
        [Fact]
        public async Task Outbound_DefaultSettings_SerializedAndProduced()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName)))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Subscriber.OutboundEnvelopes.Count.Should().Be(1);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
        }

        [Fact]
        public async Task OutboundAndInbound_DefaultSettings_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1"
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 15).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            Subscriber.OutboundEnvelopes.Count.Should().Be(15);
            Subscriber.InboundEnvelopes.Count.Should().Be(15);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(15);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(15);

            var receivedContents =
                SpyBehavior.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).Content);

            receivedContents.Should()
                .BeEquivalentTo(Enumerable.Range(1, 15).Select(i => i.ToString(CultureInfo.InvariantCulture)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Inbound_WithAndWithoutAutoCommit_OffsetCommitted(bool enableAutoCommit)
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = enableAutoCommit,
                                            AutoCommitIntervalMs = 50,
                                            CommitOffsetEach = enableAutoCommit ? -1 : 5
                                        }
                                    })))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "one"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "two"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "three"
                });

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            await AsyncTestingUtil.WaitAsync(() => DefaultTopic.GetCommittedOffsetsCount("consumer1") == 3);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task InboundAndOutbound_MessageWithCustomHeaders_HeadersTransferred()
        {
            var message = new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(new KafkaConsumerEndpoint(DefaultTopicName)
                                {
                                    Configuration = new KafkaConsumerConfig
                                    {
                                        GroupId = "consumer1",
                                    }
                                }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header", "Hello header!"));
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header2", "False"));
        }
    }
}
