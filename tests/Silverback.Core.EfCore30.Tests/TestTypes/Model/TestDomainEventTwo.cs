// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;

// ReSharper disable once CheckNamespace
namespace Silverback.Tests.Core.EFCore30.TestTypes.Model;

public class TestDomainEventTwo : DomainEvent<TestAggregateRoot>
{
    public string? Message { get; set; }
}
