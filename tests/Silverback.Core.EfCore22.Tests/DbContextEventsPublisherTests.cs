// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.EFCore30.TestTypes;
using Silverback.Tests.Core.EFCore30.TestTypes.Model;
using Xunit;

namespace Silverback.Tests.Core.EFCore22
{
    public sealed class DbContextEventsPublisherTests : IAsyncDisposable
    {
        private readonly TestDbContext _dbContext;

        private readonly IPublisher _publisher;

        private readonly SqliteConnection _connection;

        public DbContextEventsPublisherTests()
        {
            _publisher = Substitute.For<IPublisher>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new TestDbContext(dbOptions, _publisher);

            _dbContext.Database.EnsureCreated();
        }

        [Fact]
        public void SaveChanges_SomeEventsAdded_PublishCalled()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();

            _dbContext.SaveChanges();

            _publisher.Received(1).Publish(Arg.Any<IEnumerable<object>>());
        }

        [Fact]
        public async Task SaveChangesAsync_SomeEventsAdded_PublishCalled()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();

            await _dbContext.SaveChangesAsync();

            await _publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<object>>());
        }

        [Fact]
        public void SaveChanges_SomeEventsAdded_PublishingChainCalled()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _publisher
                .When(x => x.Publish(Arg.Any<IEnumerable<object>>()))
                .Do(
                    x =>
                    {
                        if (x.Arg<IEnumerable<object>>().FirstOrDefault() is TestDomainEventOne)
                            entity.Entity.AddEvent<TestDomainEventTwo>();
                    });

            entity.Entity.AddEvent<TestDomainEventOne>();

            _dbContext.SaveChanges();

            _publisher.Received(2).Publish(Arg.Any<IEnumerable<object>>());
        }

        [Fact]
        public async Task SaveChangesAsync_SomeEventsAdded_PublishingChainCalled()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _publisher
                .When(x => x.PublishAsync(Arg.Any<IEnumerable<object>>()))
                .Do(
                    x =>
                    {
                        if (x.Arg<IEnumerable<object>>().FirstOrDefault() is TestDomainEventOne)
                            entity.Entity.AddEvent<TestDomainEventTwo>();
                    });

            entity.Entity.AddEvent<TestDomainEventOne>();

            await _dbContext.SaveChangesAsync();

            await _publisher.Received(2).PublishAsync(Arg.Any<IEnumerable<object>>());
        }

        [Fact]
        public async Task SaveChangesAsync_Successful_StartedAndCompleteEventsFired()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            entity.Entity.AddEvent<TestDomainEventOne>();

            await _dbContext.SaveChangesAsync();

            await _publisher.Received(1).PublishAsync(Arg.Any<TransactionStartedEvent>());
            await _publisher.Received(1).PublishAsync(Arg.Any<TransactionCompletedEvent>());
        }

        [Fact]
        public async Task SaveChangesAsync_Error_StartedAndAbortedEventsFired()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _publisher
                .When(x => x.PublishAsync(Arg.Any<IEnumerable<object>>()))
                .Do(
                    x =>
                    {
                        if (x.Arg<IEnumerable<object>>().FirstOrDefault() is TestDomainEventOne)
                            throw new InvalidOperationException();
                    });

            entity.Entity.AddEvent<TestDomainEventOne>();

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                // ignored
            }

            await _publisher.Received(1).PublishAsync(Arg.Any<TransactionStartedEvent>());
            await _publisher.Received(1).PublishAsync(Arg.Any<TransactionAbortedEvent>());
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection == null)
                return;

            _connection.Close();
            await _connection.DisposeAsync();

            _dbContext.Dispose();
        }
    }
}
