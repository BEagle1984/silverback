// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Core.EFCore22.TestTypes.Base.Domain;

namespace Silverback.Tests.Core.EFCore22.TestTypes.Model
{
    public class TestDomainEventOne : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}