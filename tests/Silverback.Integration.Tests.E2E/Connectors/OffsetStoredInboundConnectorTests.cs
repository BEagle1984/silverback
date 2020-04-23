// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Connectors
{
    [Trait("Category", "E2E")]
    public class OffsetStoredInboundConnectorTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private readonly ServiceProvider _serviceProvider;

        private readonly BusConfigurator _configurator;
        private readonly OutboundInboundSubscriber _subscriber;

        public OffsetStoredInboundConnectorTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddDbContext<TestDbContext>(options => options
                    .UseSqlite(_connection))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker()
                    .AddDbOffsetStoredInboundConnector())
                .UseDbContext<TestDbContext>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
        }

        [Fact]
        public async Task DuplicatedMessages_ConsumedOnce()
        {
            var message1 = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers1 = new MessageHeaderCollection();
            var rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                headers1,
                MessageSerializationContext.Empty);

            var message2 = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers2 = new MessageHeaderCollection();
            var rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                headers2,
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            var consumer = (InMemoryConsumer) broker.Consumers.First();
            await consumer.Receive(rawMessage1, headers1, new InMemoryOffset("test-e2e", 1));
            await consumer.Receive(rawMessage2, headers2, new InMemoryOffset("test-e2e", 2));
            await consumer.Receive(rawMessage1, headers1, new InMemoryOffset("test-e2e", 1));
            await consumer.Receive(rawMessage2, headers2, new InMemoryOffset("test-e2e", 2));

            _subscriber.InboundEnvelopes.Count.Should().Be(2);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            _subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}