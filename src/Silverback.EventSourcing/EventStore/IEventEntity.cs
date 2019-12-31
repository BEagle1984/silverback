// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.EventStore
{
    public interface IEventEntity
    {
        DateTime Timestamp { get; set; }

        int Sequence { get; set; }

        string SerializedEvent { get; set; }
    }
}