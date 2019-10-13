// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Domain;

namespace Silverback.EventStore
{
    public interface IEventSourcingAggregate
    {
        int GetVersion();

        IEnumerable<IEntityEvent> GetNewEvents();
    }

    public interface IEventSourcingAggregate<out TKey> : IEventSourcingAggregate
    {
        TKey Id { get; }
    }
}