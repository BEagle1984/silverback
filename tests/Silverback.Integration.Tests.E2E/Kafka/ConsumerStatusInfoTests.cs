// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ConsumerStatusInfoTests : KafkaTestFixture
    {
        public ConsumerStatusInfoTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task StatusInfo_ConsumingAndDisconnecting_StatusIsCorrectlySet()
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
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var broker = Host.ScopedServiceProvider.GetRequiredService<IBroker>();

            broker.Consumers[0].IsConnected.Should().BeTrue();
            broker.Consumers[0].StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            broker.Consumers[0].IsConnected.Should().BeTrue();
            broker.Consumers[0].StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

            await Helper.Broker.DisconnectAsync();

            broker.Consumers[0].IsConnected.Should().BeFalse();
            broker.Consumers[0].StatusInfo.Status.Should().Be(ConsumerStatus.Disconnected);
        }

        [Fact]
        public async Task StatusInfo_ConsumingAndDisconnecting_StatusHistoryRecorded()
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
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var broker = Host.ScopedServiceProvider.GetRequiredService<IBroker>();

            broker.Consumers[0].StatusInfo.History.Should().HaveCount(1);
            broker.Consumers[0].StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Connected);
            broker.Consumers[0].StatusInfo.History.Last().Timestamp.Should().NotBeNull();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            broker.Consumers[0].StatusInfo.History.Should().HaveCount(2);
            broker.Consumers[0].StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Consuming);
            broker.Consumers[0].StatusInfo.History.Last().Timestamp.Should().NotBeNull();

            await Helper.Broker.DisconnectAsync();

            broker.Consumers[0].StatusInfo.History.Should().HaveCount(3);
            broker.Consumers[0].StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Disconnected);
            broker.Consumers[0].StatusInfo.History.Last().Timestamp.Should().NotBeNull();
        }

        [Fact]
        public async Task StatusInfo_Consuming_LatestConsumedMessageTracked()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
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
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            var broker = Host.ScopedServiceProvider.GetRequiredService<IBroker>();
            broker.Consumers[0].StatusInfo.LatestConsumedMessageIdentifier.Should().BeOfType<KafkaOffset>();
            broker.Consumers[0].StatusInfo.LatestConsumedMessageIdentifier.As<KafkaOffset>().Offset.Should().Be(1);
            broker.Consumers[0].StatusInfo.LatestConsumedMessageTimestamp.Should().NotBeNull();
        }
    }
}
