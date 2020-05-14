// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    /// <summary>
    ///     The base class for the domain entities that encapsulate domain events.
    /// </summary>
    /// <remarks>
    ///     It's not mandatory to use this base class as long as long as the domain entities implement
    ///     the <see cref="IMessagesSource"/> interface.
    /// </remarks>
    public abstract class DomainEntity : MessagesSource<IDomainEvent>
    {
        /// <summary>
        ///     Gets the <see cref="IDomainEvent{TEntity}"/> events that have been added but not
        ///     yet published.
        /// </summary>
        [NotMapped]
        public IEnumerable<IDomainEvent> DomainEvents =>
            GetMessages()?.Cast<IDomainEvent>() ?? Enumerable.Empty<IDomainEvent>();
    }
}