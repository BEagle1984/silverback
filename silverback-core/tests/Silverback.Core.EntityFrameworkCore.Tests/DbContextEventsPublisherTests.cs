// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Core.EntityFrameworkCore.Tests.TestTypes;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Xunit;

namespace Silverback.Core.EntityFrameworkCore.Tests
{
    public class DbContextEventsPublisherTests
    {
        private readonly TestDbContext _dbContext;
        private readonly IPublisher _publisher;

        public DbContextEventsPublisherTests()
        {
            _publisher = Substitute.For<IPublisher>();

            var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDbContext")
                .Options;

            _dbContext = new TestDbContext(dbOptions, _publisher);
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

            _publisher.Received(3).Publish(Arg.Any<TestDomainEventOne>());
            _publisher.Received(2).Publish(Arg.Any<TestDomainEventTwo>());
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

            await _publisher.Received(3).PublishAsync(Arg.Any<TestDomainEventOne>());
            await _publisher.Received(2).PublishAsync(Arg.Any<TestDomainEventTwo>());
        }

        [Fact]
        public void SaveChanges_SomeEventsAdded_PublishingChainCalled()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _publisher
                .When(x => x.Publish(Arg.Any<TestDomainEventOne>()))
                .Do(x => entity.Entity.AddEvent<TestDomainEventTwo>());

            entity.Entity.AddEvent<TestDomainEventOne>();

            _dbContext.SaveChanges();

            _publisher.Received(1).Publish(Arg.Any<TestDomainEventOne>());
            _publisher.Received(1).Publish(Arg.Any<TestDomainEventTwo>());
        }

        [Fact]
        public async Task SaveChangesAsync_SomeEventsAdded_PublishingChainCalled()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _publisher
                .When(x => x.PublishAsync(Arg.Any<TestDomainEventOne>()))
                .Do(x => entity.Entity.AddEvent<TestDomainEventTwo>());

            entity.Entity.AddEvent<TestDomainEventOne>();

            await _dbContext.SaveChangesAsync();

            await _publisher.Received(1).PublishAsync(Arg.Any<TestDomainEventOne>());
            await _publisher.Received(1).PublishAsync(Arg.Any<TestDomainEventTwo>());
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
                .When(x => x.PublishAsync(Arg.Any<TestDomainEventOne>()))
                .Do(x => throw new Exception());


            entity.Entity.AddEvent<TestDomainEventOne>();

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
            }

            await _publisher.Received(1).PublishAsync(Arg.Any<TransactionStartedEvent>());
            await _publisher.Received(1).PublishAsync(Arg.Any<TransactionAbortedEvent>());
        }
    }
}
