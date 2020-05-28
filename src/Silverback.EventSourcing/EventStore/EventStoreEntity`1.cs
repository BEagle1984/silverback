// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.EventStore
{
    /// <inheritdoc cref="Silverback.EventStore.IEventStoreEntity{TEventEntity}" />
    public class EventStoreEntity<TEventEntity> : MessagesSource<object>, IEventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity
    {
        /// <inheritdoc />
        public ICollection<TEventEntity> Events { get; } = new HashSet<TEventEntity>(); // TODO: HashSet?

        /// <inheritdoc />
        public int EntityVersion { get; set; }

        /// <inheritdoc />
        public void AddDomainEvents(IEnumerable<object> events) => events?.ForEach(AddEvent);
    }
}
