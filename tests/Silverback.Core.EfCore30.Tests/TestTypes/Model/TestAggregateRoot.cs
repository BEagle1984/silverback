// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;
using Silverback.Domain;

// ReSharper disable once CheckNamespace
namespace Silverback.Tests.Core.EFCore30.TestTypes.Model;

public class TestAggregateRoot : DomainEntity
{
    [Key]
    public int Id { get; set; }

    public new void AddEvent(IDomainEvent domainEvent)
        => base.AddEvent(domainEvent);

    public TEvent AddEvent<TEvent>()
        where TEvent : IDomainEvent, new()
        => base.AddEvent<TEvent>();
}
