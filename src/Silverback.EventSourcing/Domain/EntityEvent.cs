// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;

namespace Silverback.Domain
{
    public abstract class EntityEvent : IEntityEvent
    {
        [JsonIgnore] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonIgnore] public int Sequence { get; set; } = 0;
    }
}