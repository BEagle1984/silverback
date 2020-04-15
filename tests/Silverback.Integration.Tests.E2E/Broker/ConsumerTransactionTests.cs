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
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E"), Collection("StaticInMemory")]
    public class ConsumerTransactionTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly BusConfigurator _configurator;

        public ConsumerTransactionTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker()
                    .AddChunkStore<InMemoryChunkStore>())
                ;

            _serviceProvider = services.BuildServiceProvider();

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();

            InMemoryChunkStore.Clear();
        }

        [Fact]
        public async Task MultipleMessages_EachOffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };

            var broker = _configurator
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")))
                .First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

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

            var broker = _configurator
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e"),
                        settings: new InboundConnectorSettings
                        {
                            Batch = new BatchSettings
                            {
                                Size = 3
                            }
                        }))
                .First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

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

            var broker = _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e"),
                        errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)))
                .First();

            var consumer = (InMemoryConsumer) broker.Consumers.First();
            consumer.CommitCalled += (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);
            consumer.RollbackCalled += (_, offsetsCollection) => rolledBackOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

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

            _configurator
                .Subscribe((ISilverbackEvent silverbackEvent) => { silverbackEvents.Add(silverbackEvent); })
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    silverbackEvents.OfType<ConsumingCompletedEvent>().Should().BeEmpty();
                    silverbackEvents.OfType<ConsumingAbortedEvent>().Count().Should().Be(tryCount);

                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e"),
                        errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            tryCount.Should().Be(3);
            silverbackEvents.OfType<ConsumingAbortedEvent>().Count().Should().Be(2);
            silverbackEvents.OfType<ConsumingCompletedEvent>().Count().Should().Be(1);
        }

        [Fact]
        public async Task FailedProcessing_RolledBackOffsetOnce()
        {
            var rolledBackOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var tryCount = 0;

            var broker = _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;

                    throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e"),
                        errorPolicy => errorPolicy.Retry().MaxFailedAttempts(2)))
                .First();

            var consumer = (InMemoryConsumer) broker.Consumers.First();
            consumer.RollbackCalled += (_, offsetsCollection) => rolledBackOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

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
    }
}