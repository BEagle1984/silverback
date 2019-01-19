// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Core.EntityFrameworkCore.Tests.TestTypes.Base.Domain;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDomainEventTwo : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}