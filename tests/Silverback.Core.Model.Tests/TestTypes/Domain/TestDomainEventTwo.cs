// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;

namespace Silverback.Tests.Core.Model.TestTypes.Domain;

public class TestDomainEventTwo : DomainEvent<TestAggregateRoot>
{
    public string? Message { get; set; }
}
