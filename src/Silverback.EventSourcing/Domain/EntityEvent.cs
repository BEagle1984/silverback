// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json.Serialization;

namespace Silverback.Domain
{
    /// <inheritdoc cref="IEntityEvent" />
    public abstract class EntityEvent : IEntityEvent
    {
        /// <inheritdoc cref="IEntityEvent.Timestamp" />
        [JsonIgnore]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <inheritdoc cref="IEntityEvent.Sequence" />
        [JsonIgnore]
        public int Sequence { get; set; } = 0;
    }
}
