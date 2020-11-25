// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class BasicTests : E2ETestFixture
    {
        public BasicTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName)))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Subscriber.OutboundEnvelopes.Should().HaveCount(1);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(1);
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(15);
            Subscriber.InboundEnvelopes.Should().HaveCount(15);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(15);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(15);
            SpyBehavior.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleTopicsForDifferentMessages_ProducedAndConsumed()
        {
            var receivedEvents = new List<IEvent>();
            var receivedTestEventOnes = new List<TestEventOne>();
            var receivedTestEventTwos = new List<TestEventTwo>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne>(new KafkaProducerEndpoint("topic1"))
                                .AddOutbound<TestEventTwo>(new KafkaProducerEndpoint("topic2"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("topic1")
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("topic2")
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IInboundEnvelope<IEvent> envelope) =>
                            {
                                lock (receivedEvents)
                                {
                                    receivedEvents.Add(envelope.Message!);
                                }
                            })
                        .AddDelegateSubscriber(
                            (TestEventOne message) =>
                            {
                                lock (receivedTestEventOnes)
                                {
                                    receivedTestEventOnes.Add(message);
                                }
                            })
                        .AddDelegateSubscriber(
                            (TestEventTwo message) =>
                            {
                                lock (receivedTestEventTwos)
                                {
                                    receivedTestEventTwos.Add(message);
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventTwo
                    {
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventThree
                    {
                        Content = $"{i}"
                    });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedEvents.Should().HaveCount(10);
            receivedTestEventOnes.Should().HaveCount(5);
            receivedTestEventTwos.Should().HaveCount(5);

            GetTopic("topic1").GetCommittedOffsetsCount("consumer1").Should().Be(5);
            GetTopic("topic2").GetCommittedOffsetsCount("consumer1").Should().Be(5);
        }

        [Fact]
        public async Task OutboundAndInbound_WithHardcodedMessageType_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Serializer = new JsonMessageSerializer<TestEventOne>()
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = new JsonMessageSerializer<TestEventOne>()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(1);
            Subscriber.InboundEnvelopes.Should().HaveCount(1);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);

            SpyBehavior.OutboundEnvelopes[0].Headers.Should()
                .NotContain(header => header.Name == DefaultMessageHeaders.MessageType);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Inbound_WithHardcodedMessageType_MessageTypeHeaderIgnored()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        Serializer = new JsonMessageSerializer<TestEventOne>()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(DefaultTopicName));
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Silverback.Bad.TestEventOne, Silverback.Bad" }
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);

            SpyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleTopics_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("topic1"))
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("topic2"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("topic1")
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("topic2")
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(10);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(10);

            var receivedContentsTopic1 =
                SpyBehavior.InboundEnvelopes
                    .Where(envelope => envelope.Endpoint.Name == "topic1")
                    .Select(envelope => ((TestEventOne)envelope.Message!).Content);
            var receivedContentsTopic2 =
                SpyBehavior.InboundEnvelopes
                    .Where(envelope => envelope.Endpoint.Name == "topic2")
                    .Select(envelope => ((TestEventOne)envelope.Message!).Content);

            var expectedMessages =
                Enumerable.Range(1, 5).Select(i => $"{i}").ToList();

            receivedContentsTopic1.Should().BeEquivalentTo(expectedMessages);
            receivedContentsTopic2.Should().BeEquivalentTo(expectedMessages);
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleTopicsWithSingleConsumer_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("topic1"))
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("topic2"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("topic1", "topic2")
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(10);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(10);

            var receivedContentsTopic1 =
                SpyBehavior.InboundEnvelopes
                    .Where(envelope => envelope.ActualEndpointName == "topic1")
                    .Select(envelope => ((TestEventOne)envelope.Message!).Content);
            var receivedContentsTopic2 =
                SpyBehavior.InboundEnvelopes
                    .Where(envelope => envelope.ActualEndpointName == "topic2")
                    .Select(envelope => ((TestEventOne)envelope.Message!).Content);

            var expectedMessages =
                Enumerable.Range(1, 5).Select(i => $"{i}").ToList();

            receivedContentsTopic1.Should().BeEquivalentTo(expectedMessages);
            receivedContentsTopic2.Should().BeEquivalentTo(expectedMessages);
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleConsumersSameConsumerGroup_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(10);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(10);
            SpyBehavior.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Distinct()
                .Should().BeEquivalentTo(Enumerable.Range(1, 10).Select(i => $"{i}"));
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleConsumerInstances_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    },
                                    2))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(10);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(10);
            SpyBehavior.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Distinct()
                .Should().BeEquivalentTo(Enumerable.Range(1, 10).Select(i => $"{i}"));
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                            CommitOffsetEach = enableAutoCommit ? -1 : 3
                                        }
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
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

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task OutboundAndInbound_MessageWithCustomHeaders_HeadersTransferred()
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
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            SpyBehavior.InboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header", "Hello header!"));
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header2", "False"));
        }

        [Fact]
        public async Task Inbound_ThrowIfUnhandled_ConsumerStoppedIfMessageIsNotHandled()
        {
            var received = 0;
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        ThrowIfUnhandled = true
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber((TestEventOne _) => received++))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Handled message"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            received.Should().Be(1);

            await publisher.PublishAsync(
                new TestEventTwo
                {
                    Content = "Unhandled message"
                });

            await AsyncTestingUtil.WaitAsync(() => Broker.Consumers[0].IsConnected == false);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task DisconnectAsync_WithoutAutoCommit_PendingOffsetsCommitted()
        {
            int receivedMessages = 0;
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            AutoCommitIntervalMs = 50,
                                            CommitOffsetEach = 10
                                        }
                                    }))
                        .AddDelegateSubscriber((TestEventOne _) => receivedMessages++))
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

            await AsyncTestingUtil.WaitAsync(() => receivedMessages == 3);

            await Broker.DisconnectAsync();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task Rebalance_WithoutAutoCommit_PendingOffsetsCommitted()
        {
            int receivedMessages = 0;
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            AutoCommitIntervalMs = 50,
                                            CommitOffsetEach = 10
                                        }
                                    }))
                        .AddDelegateSubscriber((TestEventOne _) => receivedMessages++))
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

            await AsyncTestingUtil.WaitAsync(() => receivedMessages == 3);

            DefaultTopic.Rebalance();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task Rebalance_DefaultSettings_ProducedAndConsumedAfterRebalance()
        {
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(5);
            Subscriber.InboundEnvelopes.Should().HaveCount(5);

            DefaultTopic.Rebalance();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);
        }

        [Fact]
        public async Task StopAndStart_DefaultSettings_MessagesConsumedAfterRestart()
        {
            var serviceProvider = Host.ConfigureServices(
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
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(5);
            Subscriber.InboundEnvelopes.Should().HaveCount(5);

            Broker.Consumers[0].Stop();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await Task.Delay(200);

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(5);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(5);

            Broker.Consumers[0].Start();

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(10);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);
        }

        [Fact]
        public async Task Inbound_FromMultiplePartitionsWithLimitedParallelism_ConcurrencyLimited()
        {
            var receivedMessages = new List<TestEventWithKafkaKey>();
            var taskCompletionSource = new TaskCompletionSource<bool>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        MaxDegreeOfParallelism = 2
                                    }))
                        .AddDelegateSubscriber(
                            async (TestEventWithKafkaKey message) =>
                            {
                                lock (receivedMessages)
                                {
                                    receivedMessages.Add(message);
                                }

                                await taskCompletionSource.Task;
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 3; i++)
            {
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 1,
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 2,
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 3,
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 4,
                        Content = $"{i}"
                    });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
            await Task.Delay(100);

            try
            {
                receivedMessages.Should().HaveCount(2);
            }
            finally
            {
                taskCompletionSource.SetResult(true);
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(12);
        }
    }
}
