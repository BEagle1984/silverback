// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    public abstract class DomainEntity : MessagesSource<IDomainEvent>
    {
        [NotMapped]
        public IEnumerable<IDomainEvent> DomainEvents =>
            GetMessages()?.Cast<IDomainEvent>() ?? Enumerable.Empty<IDomainEvent>();
    }
}
