// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
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
    public class BasicTests : KafkaTestFixture
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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Subscriber.OutboundEnvelopes.Should().HaveCount(1);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
        }

        [Fact]
        public async Task OutboundAndInbound_DefaultSettings_ProducedAndConsumed()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .AddOutbound<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic2")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
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

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedEvents.Should().HaveCount(10);
            receivedTestEventOnes.Should().HaveCount(5);
            receivedTestEventTwos.Should().HaveCount(5);

            GetTopic("topic1").GetCommittedOffsetsCount("consumer1").Should().Be(5);
            GetTopic("topic2").GetCommittedOffsetsCount("consumer1").Should().Be(5);
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleTopics_ProducedAndConsumed()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo("topic1"))
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo("topic2"))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic2")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo("topic1"))
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo("topic2"))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1", "topic2")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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
        public async Task OutboundAndInbound_MultipleConsumersDifferentConsumerGroup_ProducedAndConsumed()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer2";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(20);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);
            DefaultTopic.GetCommittedOffsetsCount("consumer2").Should().Be(10);
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleConsumerInstances_ProducedAndConsumed()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            }),
                                    2))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = enableAutoCommit;
                                                config.AutoCommitIntervalMs = 50;
                                                config.CommitOffsetEach = enableAutoCommit ? -1 : 3;
                                            })))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })
                                        .ThrowIfUnhandled()))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber((TestEventOne _) => received++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Handled message"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();
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
        public async Task Inbound_IgnoreUnhandledMessages_UnhandledMessageIgnored()
        {
            var received = 0;
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })
                                        .IgnoreUnhandledMessages()))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddDelegateSubscriber((TestEventOne _) => received++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Handled message"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            received.Should().Be(1);

            await publisher.PublishAsync(
                new TestEventTwo
                {
                    Content = "Unhandled message"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            received.Should().Be(1);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task DisconnectAsync_WithoutAutoCommit_PendingOffsetsCommitted()
        {
            int receivedMessages = 0;
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 10;
                                            })))
                        .AddDelegateSubscriber((TestEventOne _) => receivedMessages++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 10;
                                            })))
                        .AddDelegateSubscriber((TestEventOne _) => receivedMessages++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(10);
            Subscriber.InboundEnvelopes.Should().HaveCount(10);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);
        }

        [Fact]
        public async Task StopAsyncAndStartAsync_DefaultSettings_MessagesConsumedAfterRestart()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(5);
            Subscriber.InboundEnvelopes.Should().HaveCount(5);

            await Broker.Consumers[0].StopAsync();

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

            await Broker.Consumers[0].StartAsync();

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(10);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);
        }

        [Fact]
        public async Task Inbound_FromMultiplePartitionsWithLimitedParallelism_ConcurrencyLimited()
        {
            var receivedMessages = new List<TestEventWithKafkaKey>();
            var taskCompletionSource = new TaskCompletionSource<bool>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })
                                        .LimitParallelism(2)))
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

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(12);
        }
    }
}
