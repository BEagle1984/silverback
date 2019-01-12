// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using Silverback.Core.EntityFrameworkCore.Tests.TestTypes;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.EntityFrameworkCore.Tests
{
    [TestFixture]
    public class DbContextEventsPublisherTests
    {
        private TestDbContext _dbContext;
        private IEventPublisher _eventPublisher;

        [SetUp]
        public void Setup()
        {
            _eventPublisher = Substitute.For<IEventPublisher>();

            var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDbContext")
                .Options;

            _dbContext = new TestDbContext(dbOptions, _eventPublisher);
        }

        [Test]
        public void EventsPublishingTest()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();

            _dbContext.SaveChanges();

            _eventPublisher.Received(3).Publish(Arg.Any<TestDomainEventOne>());
            _eventPublisher.Received(2).Publish(Arg.Any<TestDomainEventTwo>());
        }

        [Test]
        public async Task AsyncEventsPublishingTest()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();
            entity.Entity.AddEvent<TestDomainEventTwo>();
            entity.Entity.AddEvent<TestDomainEventOne>();

            await _dbContext.SaveChangesAsync();

            await _eventPublisher.Received(3).PublishAsync(Arg.Any<TestDomainEventOne>());
            await _eventPublisher.Received(2).PublishAsync(Arg.Any<TestDomainEventTwo>());
        }

        [Test]
        public void EventsPublishingChainTest()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _eventPublisher
                .When(x => x.Publish(Arg.Any<TestDomainEventOne>()))
                .Do(x => entity.Entity.AddEvent<TestDomainEventTwo>());

            entity.Entity.AddEvent<TestDomainEventOne>();

            _dbContext.SaveChanges();

            _eventPublisher.Received(1).Publish(Arg.Any<TestDomainEventOne>());
            _eventPublisher.Received(1).Publish(Arg.Any<TestDomainEventTwo>());
        }

        [Test]
        public async Task AsyncEventsPublishingChainTest()
        {
            var entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

            _eventPublisher
                .When(x => x.PublishAsync(Arg.Any<TestDomainEventOne>()))
                .Do(x => entity.Entity.AddEvent<TestDomainEventTwo>());

            entity.Entity.AddEvent<TestDomainEventOne>();

            await _dbContext.SaveChangesAsync();

            await _eventPublisher.Received(1).PublishAsync(Arg.Any<TestDomainEventOne>());
            await _eventPublisher.Received(1).PublishAsync(Arg.Any<TestDomainEventTwo>());
        }
    }
}
