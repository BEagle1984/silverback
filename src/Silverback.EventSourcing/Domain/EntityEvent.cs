// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;

namespace Silverback.Domain
{
    /// <inheritdoc cref="IEntityEvent"/>
    public abstract class EntityEvent : IEntityEvent
    {
        /// <inheritdoc />
        [JsonIgnore]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <inheritdoc />
        [JsonIgnore]
        public int Sequence { get; set; } = 0;
    }
}