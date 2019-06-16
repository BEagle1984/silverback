// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Core.EntityFrameworkCore.TestTypes.Base.Domain
{
    public interface IDomainEvent : IEvent
    {
        object Source { get; set; }
    }

    public interface IDomainEvent<out TEntity> : IDomainEvent
    {
        new TEntity Source { get; }
    }
}