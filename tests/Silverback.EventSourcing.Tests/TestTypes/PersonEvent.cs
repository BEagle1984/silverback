// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonEvent : EventEntity
    {
        [Key]
        public int Id { get; private set; }
    }
}