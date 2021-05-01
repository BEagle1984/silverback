// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Domain;
using Silverback.Tests.EventSourcing.TestTypes.DomainEvents;
using Silverback.Tests.EventSourcing.TestTypes.EntityEvents;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class Person : EventSourcingDomainEntity<int, PersonDomainEvent>
    {
        public Person()
        {
        }

        public Person(int id)
        {
            Id = id;
        }

        public Person(IReadOnlyCollection<IEntityEvent> events)
            : base(events)
        {
        }

        public string? Ssn { get; private set; }

        public string? Name { get; private set; }

        public int Age { get; private set; }

        public string? PhoneNumber { get; private set; }

        public void ChangeName(string newName) =>
            AddAndApplyEvent(
                new NameChangedEvent
                {
                    NewName = newName
                });

        public void ChangeAge(int newAge) =>
            AddAndApplyEvent(
                new AgeChangedEvent
                {
                    NewAge = newAge
                });

        public void ChangePhoneNumber(string newPhoneNumber) =>
            AddAndApplyEvent(
                new PhoneNumberChangedEvent
                {
                    NewPhoneNumber = newPhoneNumber
                });

        public IEnumerable<IEntityEvent> MergeEvents(IEnumerable<IEntityEvent> events) =>
            events.Select(AddAndApplyEvent).ToList();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private void Apply(NameChangedEvent @event, bool isReplaying)
        {
            // Skip if a newer event exists (just to show how it can be done)
            if (!isReplaying && Events.Any(e => e is NameChangedEvent && e.Timestamp > @event.Timestamp))
                return;

            Name = @event.NewName;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private void Apply(AgeChangedEvent @event) => Age = @event.NewAge;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private void Apply(PhoneNumberChangedEvent @event, bool isReplaying)
        {
            PhoneNumber = @event.NewPhoneNumber;

            if (isReplaying)
                PhoneNumber += "*";
        }
    }
}
