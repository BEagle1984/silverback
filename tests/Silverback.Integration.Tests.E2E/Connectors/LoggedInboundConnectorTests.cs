// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Connectors
{
    [Trait("Category", "E2E")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Connection scoped to test method")]
    public class LoggedInboundConnectorTests : E2ETestFixture
    {
        [Fact]
        public async Task DuplicatedMessages_ConsumedOnce()
        {
            var message1 = new TestEventWithUniqueKey
            {
                UniqueKey = "some-unique-key",
                Content = "Hello E2E!"
            };
            var message2 = new TestEventWithUniqueKey
            {
                UniqueKey = "some-other-unique-key",
                Content = "Hello E2E!"
            };

            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddDbContext<TestDbContext>(
                            options => options
                                .UseSqlite(connection))
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddDbLoggedInboundConnector())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .UseDbContext<TestDbContext>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);
            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            Subscriber.OutboundEnvelopes.Count.Should().Be(4);
            Subscriber.InboundEnvelopes.Count.Should().Be(2);
            Subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            Subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }
    }
}
