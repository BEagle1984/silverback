// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.EventStore
{
    /// <inheritdoc />
    public abstract class EventEntity : IEventEntity
    {
        /// <inheritdoc />
        public DateTime Timestamp { get; set; }

        /// <inheritdoc />
        public int Sequence { get; set; }

        /// <inheritdoc />
        public string SerializedEvent { get; set; } = null!;
    }
}
