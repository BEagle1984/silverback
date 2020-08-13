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
using Silverback.Messaging.Connectors;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e"))))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            committedOffsets.Count.Should().Be(1);

            await publisher.PublishAsync(message);
            committedOffsets.Count.Should().Be(2);

            await publisher.PublishAsync(message);
            committedOffsets.Count.Should().Be(3);
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    settings: new InboundConnectorSettings
                                    {
                                        Batch = new BatchSettings
                                        {
                                            Size = 3
                                        }
                                    })))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            committedOffsets.Should().BeEmpty();

            await publisher.PublishAsync(message);
            committedOffsets.Should().BeEmpty();

            await publisher.PublishAsync(message);
            committedOffsets.Count.Should().Be(3);
        }

        [Fact]
        public async Task WithFailuresAndRetryPolicy_NoOffsetRollbacksAndCommittedOnce()
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);
            consumer.RollbackCalled += (_, args) => rolledBackOffsets.AddRange(args.Offsets);

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            committedOffsets.Count.Should().Be(1);
            rolledBackOffsets.Should().BeEmpty();
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)))
                        .AddDelegateSubscriber(
                            (ISilverbackEvent silverbackEvent) =>
                            {
                                silverbackEvents.Add(silverbackEvent);
                            })
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
        public async Task ChunkingWithFailuresAndRetryPolicy_NoOffsetRollbacksAndCommittedOnce()
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings { Size = 10 }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);
            consumer.RollbackCalled += (_, args) => rolledBackOffsets.AddRange(args.Offsets);

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            committedOffsets.Count.Should().Be(3);
            rolledBackOffsets.Should().BeEmpty();
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings { Size = 10 }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)))
                        .AddDelegateSubscriber(
                            (ISilverbackEvent silverbackEvent) =>
                            {
                                silverbackEvents.Add(silverbackEvent);
                            })
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
        public async Task FailedProcessing_RolledBackOffsetOnce()
        {
            var rolledBackOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    errorPolicy => errorPolicy.Retry().MaxFailedAttempts(2)))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;

                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.RollbackCalled += (_, args) => rolledBackOffsets.AddRange(args.Offsets);

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
            rolledBackOffsets.Count.Should().Be(1);
        }

        [Fact]
        public async Task FailedChunkProcessing_RolledBackOffsetOnce()
        {
            var rolledBackOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings { Size = 10 }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e"),
                                    errorPolicy => errorPolicy.Retry().MaxFailedAttempts(2)))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;

                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.RollbackCalled += (_, args) => rolledBackOffsets.AddRange(args.Offsets);

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
            rolledBackOffsets.Count.Should().Be(3);
        }
    }
}
