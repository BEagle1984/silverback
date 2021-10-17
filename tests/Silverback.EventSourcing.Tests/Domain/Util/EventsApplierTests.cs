// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Domain;
using Silverback.Domain.Util;
using Silverback.Tests.EventSourcing.TestTypes;
using Silverback.Tests.EventSourcing.TestTypes.EntityEvents;
using Xunit;

namespace Silverback.Tests.EventSourcing.Domain.Util
{
    public class EventsApplierTests
    {
        [Fact]
        public void Apply_SingleMatchingMethod_MethodInvoked()
        {
            var entity = new Person();

            EventsApplier.Apply(new NameChangedEvent { NewName = "Silverback" }, entity);

            entity.Name.Should().Be("Silverback");
        }

        [Fact]
        public void Apply_MultipleMatchingMethods_AllMethodsInvoked()
        {
            var entity = new TestEntity();

            EventsApplier.Apply(new TestEntity.TestEntityEvent2(), entity);

            entity.Calls.Should().Be(2);
        }

        [Fact]
        public void Apply_PublicApplyMethod_MethodInvoked()
        {
            var entity = new TestEntity();

            EventsApplier.Apply(new TestEntity.TestEntityEvent1(), entity);

            entity.Calls.Should().Be(1);
        }

        [Fact]
        public void Apply_PrivateApplyMethods_MethodsInvoked()
        {
            var entity = new TestEntity();

            EventsApplier.Apply(new TestEntity.TestEntityEvent2(), entity);

            entity.Calls.Should().Be(2);
        }

        [Fact]
        public void Apply_NoMatchingMethod_ExceptionThrown()
        {
            var entity = new TestEntity();

            Action action = () => EventsApplier.Apply(new TestEntity.TestEntityEvent3(), entity);

            action.Should().Throw<SilverbackException>();
        }

        private sealed class TestEntity : EventSourcingDomainEntity<TestEntity.TestEntityEvent>
        {
            public int Calls { get; private set; }

            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            public void Apply(TestEntityEvent1 event1) => Calls++;

            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
#pragma warning disable 628
            protected void Apply(TestEntityEvent2 event2) => Calls++;
#pragma warning restore 628

            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
            [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
            private void Apply2(TestEntityEvent2 event2, bool isReplaying) => Calls++;

            public abstract class TestEntityEvent : EntityEvent
            {
            }

            public class TestEntityEvent1 : TestEntityEvent
            {
            }

            public class TestEntityEvent2 : TestEntityEvent
            {
            }

            public class TestEntityEvent3 : TestEntityEvent
            {
            }
        }
    }
}
