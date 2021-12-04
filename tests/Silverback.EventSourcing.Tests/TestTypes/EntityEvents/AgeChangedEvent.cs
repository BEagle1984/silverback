// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;

namespace Silverback.Tests.EventSourcing.TestTypes.EntityEvents;

public class AgeChangedEvent : EntityEvent
{
    public int NewAge { get; set; }
}
