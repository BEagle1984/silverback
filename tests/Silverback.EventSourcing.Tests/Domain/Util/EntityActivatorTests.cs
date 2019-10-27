// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Domain;
using Silverback.Domain.Util;
using Silverback.Tests.EventSourcing.TestTypes;
using Xunit;

namespace Silverback.Tests.EventSourcing.Domain.Util
{
    public class EntityActivatorTests
    { 
        [Fact]
        public void CreateInstance_WithSomeEvents_EntityCreated()
        {
            var events = new IEntityEvent[] { new Person.NameChangedEvent(), new Person.AgeChangedEvent() };
            var eventStoreEntity = new { };

            var entity = EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            entity.Should().NotBeNull();
            entity.Should().BeOfType<Person>();
        }

        [Fact]
        public void CreateInstance_WithSomeEvents_EventsApplied()
        {
            var events = new IEntityEvent[]
            {
                new Person.NameChangedEvent { NewName = "Silverback" },
                new Person.AgeChangedEvent { NewAge = 13 }
            };
            var eventStoreEntity = new { };

            var entity = EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            entity.Name.Should().Be("Silverback");
            entity.Age.Should().Be(13);
        }
        
        [Fact]
        public void CreateInstance_WithoutEvents_EntityCreated()
        {
            var events = new IEntityEvent[0];
            var eventStoreEntity = new { };

            var entity = EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            entity.Should().NotBeNull();
            entity.Should().BeOfType<Person>();
        }

        [Fact]
        public void CreateInstance_WithEventStoreEntity_PropertiesValuesCopiedToNewEntity()
        {
            var events = new IEntityEvent[0];
            var eventStoreEntity = new { PersonId = 1234, Ssn = "123-123 CA", EntityName = "Silverback" };

            var entity = EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            entity.Should().NotBeNull();
            entity.Id.Should().Be(1234);
            entity.Ssn.Should().Be("123-123 CA");
            entity.Name.Should().Be("Silverback");
        }
    }
}