// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class OutboxTests : E2ETestFixture
    {
        public OutboxTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task OutboxProduceStrategy_DefaultSettings_ProducedAndConsumed()
        {
            Host
                .WithTestDbContext()
                .ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .UseDbContext<TestDbContext>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddOutboxDatabaseTable()
                                .AddOutboxWorker(TimeSpan.FromMilliseconds(100)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(clientConfig => { clientConfig.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox())
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
            var dbContext = Host.ScopedServiceProvider.GetRequiredService<TestDbContext>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await dbContext.SaveChangesAsync();

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
        public async Task OutboxProduceStrategy_TransactionAborted_MessageNotProduced()
        {
            Host
                .WithTestDbContext()
                .ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .UseDbContext<TestDbContext>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddOutboxDatabaseTable()
                                .AddOutboxWorker(TimeSpan.FromMilliseconds(100)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(clientConfig => { clientConfig.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox())
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

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
                await publisher.PublishAsync(new TestEventOne());

                /* Just avoid call to SaveChanges */
            }

            var dbContext = Host.ScopedServiceProvider.GetRequiredService<TestDbContext>();
            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(1);
            Subscriber.InboundEnvelopes.Should().BeEmpty();

            dbContext.Outbox.Should().BeEmpty();
            DefaultTopic.TotalMessagesCount.Should().Be(0);
        }
    }
}
