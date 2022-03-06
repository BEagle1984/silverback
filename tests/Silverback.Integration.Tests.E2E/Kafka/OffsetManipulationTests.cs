﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Kafka;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class OffsetManipulationTests : KafkaTestFixture
    {
        public OffsetManipulationTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task PartitionsAssignedEvent_ResetOffset_MessagesConsumedAgain()
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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(config => { config.GroupId = DefaultConsumerGroupId; })))
                        .AddTransientBrokerCallbackHandler<ResetOffsetPartitionsAssignedCallbackHandler>()
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            await Helper.Broker.DisconnectAsync();
            await Helper.Broker.ConnectAsync();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 4"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(7);
        }

        [Fact]
        public async Task PartitionsAssignedEvent_NoOffsetReturned_MessagesConsumed()
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
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(config => { config.GroupId = DefaultConsumerGroupId; })))
                        .AddTransientBrokerCallbackHandler<NoResetPartitionsAssignedCallbackHandler>()
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            await Helper.Broker.DisconnectAsync();
            await Helper.Broker.ConnectAsync();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 4"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private sealed class NoResetPartitionsAssignedCallbackHandler : IKafkaPartitionsAssignedCallback
        {
            public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
                IReadOnlyCollection<TopicPartition> topicPartitions,
                KafkaConsumer consumer) => null;
        }
    }
}
