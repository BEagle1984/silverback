// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
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
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnPartitionsAssigned(
                                            (partitions, _) =>
                                                partitions.Select(
                                                    topicPartition => new TopicPartitionOffset(
                                                        topicPartition,
                                                        Offset.Beginning)))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
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
    }
}
