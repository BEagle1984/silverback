// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
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
            var serviceProvider = Host
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
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Strategy = ProduceStrategy.Outbox()
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
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            await Enumerable.Range(1, 15).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));

            await dbContext.SaveChangesAsync();

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.OutboundEnvelopes.Should().HaveCount(15);
            Subscriber.InboundEnvelopes.Should().HaveCount(15);

            SpyBehavior.OutboundEnvelopes.Should().HaveCount(15);
            SpyBehavior.InboundEnvelopes.Should().HaveCount(15);

            SpyBehavior.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => i.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
