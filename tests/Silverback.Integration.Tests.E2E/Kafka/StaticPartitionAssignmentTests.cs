// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class StaticPartitionAssignmentTests : KafkaTestFixture
    {
        public StaticPartitionAssignmentTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task StaticPartitionAssignment_PartitionsAssigned()
        {
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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo("topic1", 1))
                                .AddOutbound<TestEventTwo>(
                                    endpoint => endpoint
                                        .ProduceTo("topic1", 2))
                                .AddOutbound<TestEventThree>(
                                    endpoint => endpoint
                                        .ProduceTo("topic2", 1))
                                .AddOutbound<TestEventFour>(
                                    endpoint => endpoint
                                        .ProduceTo("topic2", 2))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(
                                            new TopicPartition("topic1", 2),
                                            new TopicPartition("topic2", 2))
                                        .Configure(config => { config.GroupId = "consumer1"; })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

            await publisher.PublishAsync(new TestEventTwo());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

            await publisher.PublishAsync(new TestEventThree());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

            await publisher.PublishAsync(new TestEventFour());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

            var topic1 = Helper.GetTopic("topic1");
            var topic2 = Helper.GetTopic("topic2");

            topic1.Partitions[0].TotalMessagesCount.Should().Be(0);
            topic1.Partitions[1].TotalMessagesCount.Should().Be(1);
            topic1.Partitions[2].TotalMessagesCount.Should().Be(1);
            topic1.Partitions[3].TotalMessagesCount.Should().Be(0);
            topic1.Partitions[4].TotalMessagesCount.Should().Be(0);
            topic2.Partitions[0].TotalMessagesCount.Should().Be(0);
            topic2.Partitions[1].TotalMessagesCount.Should().Be(1);
            topic2.Partitions[2].TotalMessagesCount.Should().Be(1);
            topic2.Partitions[3].TotalMessagesCount.Should().Be(0);
            topic2.Partitions[4].TotalMessagesCount.Should().Be(0);
        }
    }
}
