// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Connectors
{
    [Trait("Category", "E2E")]
    public class DeferredOutboundConnectorTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _serviceProvider;
        private readonly IBusConfigurator _configurator;
        private readonly OutboundInboundSubscriber _subscriber;

        public DeferredOutboundConnectorTests()
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
                    .AddDbOutboundConnector()
                    .AddDbOutboundWorker())
                .AddDbDistributedLockManager()
                .UseDbContext<TestDbContext>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

            _configurator = _serviceProvider.GetRequiredService<IBusConfigurator>();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
        }

        [Fact]
        public async Task OutboundMessages_QueuedAndPublishedByOutboundWorker()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            _subscriber.OutboundEnvelopes.Count.Should().Be(3);
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await _serviceProvider.GetRequiredService<IOutboundQueueWorker>().ProcessQueue(CancellationToken.None);

            _subscriber.InboundEnvelopes.Count.Should().Be(3);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}