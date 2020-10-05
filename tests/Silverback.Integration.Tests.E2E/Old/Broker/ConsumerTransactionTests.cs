// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E")]
    public class ConsumerTransactionTests : E2ETestFixture
    {
        [Fact]
        public async Task MultipleMessages_EachOffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(new KafkaConsumerEndpoint(DefaultTopicName))))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(1);

            await publisher.PublishAsync(message);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(2);

            await publisher.PublishAsync(message);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(3);
        }

        [Fact]
        public async Task BatchConsuming_BatchCommittedAtOnce()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };

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
                                        Batch = new BatchSettings
                                        {
                                            Size = 3
                                        }
                                    })))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(0);

            await publisher.PublishAsync(message);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(0);

            await publisher.PublishAsync(message);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(3);
        }

        [Fact]
        public async Task WithFailuresAndRetryPolicy_OffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();
            var rolledBackOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

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
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(1);
        }

        [Fact]
        public async Task WithFailuresAndRetryPolicy_CompletedOrFailedEventFiredForEachTry()
        {
            var silverbackEvents = new List<ISilverbackEvent>();
            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

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
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddDelegateSubscriber(
                            (ISilverbackEvent silverbackEvent) => { silverbackEvents.Add(silverbackEvent); })
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                silverbackEvents.OfType<ConsumingCompletedEvent>().Should().BeEmpty();
                                silverbackEvents.OfType<ConsumingAbortedEvent>().Count().Should().Be(tryCount);

                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            silverbackEvents.OfType<ConsumingAbortedEvent>().Count().Should().Be(2);
            silverbackEvents.OfType<ConsumingCompletedEvent>().Count().Should().Be(1);
        }

        [Fact]
        public async Task ChunkingWithFailuresAndRetryPolicy_OffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();
            var rolledBackOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

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
                                        Chunk = new ChunkSettings { Size = 10 }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(3);
        }

        [Fact]
        public async Task ChunkingWithFailuresAndRetryPolicy_CompletedOrFailedEventFiredForEachTry()
        {
            var silverbackEvents = new List<ISilverbackEvent>();
            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

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
                                        Chunk = new ChunkSettings { Size = 10 }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddDelegateSubscriber(
                            (ISilverbackEvent silverbackEvent) => { silverbackEvents.Add(silverbackEvent); })
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                silverbackEvents.OfType<ConsumingCompletedEvent>().Count().Should().BeLessThan(3);
                                silverbackEvents.OfType<ConsumingAbortedEvent>().Count().Should().Be(tryCount);

                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            silverbackEvents.OfType<ConsumingAbortedEvent>().Count().Should().Be(2);
            silverbackEvents.OfType<ConsumingCompletedEvent>().Count().Should().Be(3);
        }

        [Fact]
        public async Task FailedProcessing_OffsetRolledBack()
        {
            var rolledBackOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

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
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(2)
                                    }))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;

                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            try
            {
                await publisher.PublishAsync(message);
            }
            catch
            {
                // ignored
            }

            tryCount.Should().Be(3);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(0);
        }

        [Fact]
        public async Task FailedChunkProcessing_OffsetRolledBack()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

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
                                        Chunk = new ChunkSettings { Size = 10 }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(2)
                                    }))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;

                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            try
            {
                await publisher.PublishAsync(message);
            }
            catch
            {
                // ignored
            }

            tryCount.Should().Be(3);
            DefaultTopic.GetCommittedOffsets("consumer1")
                .Sum(topicPartitionOffset => topicPartitionOffset.Offset)
                .Should().Be(0);
        }
    }
}
