// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.EventStore
{
    /// <inheritdoc cref="IEventStoreEntity{TEventEntity}" />
    public class EventStoreEntity<TEventEntity> : MessagesSource<object>, IEventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity
    {
        /// <inheritdoc cref="IEventStoreEntity{TEventEntity}.Events" />
        public ICollection<TEventEntity> Events { get; } = new HashSet<TEventEntity>(); // TODO: HashSet?

        /// <inheritdoc cref="IEventStoreEntity{TEventEntity}.EntityVersion" />
        public int EntityVersion { get; set; }

        /// <inheritdoc cref="IEventStoreEntity{TEventEntity}.AddDomainEvents" />
        public void AddDomainEvents(IEnumerable<object> events) => events.ForEach(AddEvent);
    }
}
