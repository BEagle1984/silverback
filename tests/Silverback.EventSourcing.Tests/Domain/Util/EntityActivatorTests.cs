// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Domain;
using Silverback.Domain.Util;
using Silverback.Tests.EventSourcing.TestTypes;
using Silverback.Tests.EventSourcing.TestTypes.EntityEvents;
using Xunit;

namespace Silverback.Tests.EventSourcing.Domain.Util
{
    public class EntityActivatorTests
    {
        [Fact]
        public void CreateInstance_WithSomeEvents_EntityCreated()
        {
            var events = new IEntityEvent[] { new NameChangedEvent(), new AgeChangedEvent() };
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
                new NameChangedEvent { NewName = "Silverback" },
                new AgeChangedEvent { NewAge = 13 }
            };
            var eventStoreEntity = new { };

            var entity = EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            entity.Name.Should().Be("Silverback");
            entity.Age.Should().Be(13);
        }

        [Fact]
        public void CreateInstance_WithoutEvents_ExceptionThrown()
        {
            var events = Array.Empty<IEntityEvent>();
            var eventStoreEntity = new { };

            Action act = () => EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateInstance_WithEventStoreEntity_PropertiesValuesCopiedToNewEntity()
        {
            var events = new IEntityEvent[]
            {
                new AgeChangedEvent { NewAge = 13 }
            };
            var eventStoreEntity = new { PersonId = 1234, Ssn = "123-123 CA", EntityName = "Silverback" };

            var entity = EntityActivator.CreateInstance<Person>(events, eventStoreEntity);

            entity.Should().NotBeNull();
            entity.Id.Should().Be(1234);
            entity.Ssn.Should().Be("123-123 CA");
            entity.Name.Should().Be("Silverback");
        }
    }
}
