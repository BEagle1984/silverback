// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.EventStore
{
    public class EventStoreEntity<TEventEntity> : MessagesSource<object>, IEventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity
    {
        public ICollection<TEventEntity> Events { get; } = new HashSet<TEventEntity>(); // TODO: HashSet?

        public int EntityVersion { get; set; }

        public void AddDomainEvents(IEnumerable<object> events) => events?.ForEach(AddEvent);
    }
}