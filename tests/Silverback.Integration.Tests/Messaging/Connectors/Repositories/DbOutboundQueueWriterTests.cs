// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Database;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Database;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    public class DbOutboundQueueWriterTests : IAsyncDisposable
    {
        private static readonly IOutboundEnvelope SampleOutboundEnvelope = new OutboundEnvelope(
            new TestEventOne { Content = "Test" },
            new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") },
            TestProducerEndpoint.GetDefault());

        private readonly SqliteConnection _connection;

        private readonly IServiceScope _scope;

        private readonly TestDbContext _dbContext;

        private readonly DbOutboxWriter _queueWriter;

        public DbOutboundQueueWriterTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddDbContext<TestDbContext>(
                    options => options
                        .UseSqlite(_connection))
                .AddSilverback()
                .UseDbContext<TestDbContext>();

            var serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _scope = serviceProvider.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
            _dbContext.Database.EnsureCreated();

            _queueWriter = new DbOutboxWriter(_scope.ServiceProvider.GetRequiredService<IDbContext>());
        }

        [Fact]
        public void Enqueue_SomeMessages_TableStillEmpty()
        {
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);

            _dbContext.Outbox.Count().Should().Be(0);
        }

        [Fact]
        public void EnqueueCommitAndSaveChanges_SomeMessages_MessagesAddedToQueue()
        {
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.CommitAsync();
            _dbContext.SaveChanges();

            _dbContext.Outbox.Count().Should().Be(3);
        }

        [Fact]
        public void EnqueueAndRollback_SomeMessages_TableStillEmpty()
        {
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.RollbackAsync();

            _dbContext.Outbox.Count().Should().Be(0);
        }

        [Fact]
        public void EnqueueCommitAndSaveChanges_Message_MessageCorrectlyAddedToQueue()
        {
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.WriteAsync(SampleOutboundEnvelope);
            _queueWriter.CommitAsync();
            _dbContext.SaveChanges();

            var outboundMessage = _dbContext.Outbox.First();
            outboundMessage.EndpointName.Should().Be("test");
            outboundMessage.MessageType.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
            outboundMessage.Content.Should().NotBeNullOrEmpty();
            outboundMessage.SerializedHeaders.Should().NotBeNullOrEmpty();
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _connection.DisposeAsync();
            _scope.Dispose();
        }
    }
}
