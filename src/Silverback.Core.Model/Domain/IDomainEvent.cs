// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    public interface IDomainEvent : IMessageWithSource, IEvent
    {
    }

    public interface IDomainEvent<out TEntity> : IDomainEvent
    {
        new TEntity Source { get; }
    }
}