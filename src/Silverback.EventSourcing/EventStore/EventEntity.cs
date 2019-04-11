// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.EventStore
{
    public abstract class EventEntity : IEventEntity
    {
        public DateTime Timestamp { get; set; }

        public int Sequence { get; set; }

        public string SerializedEvent { get; set; }
    }
}