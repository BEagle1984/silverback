// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.EventStore
{
    /// <summary>
    ///     The stored entity that contains the information about an event applied to a domain entity.
    /// </summary>
    public interface IEventEntity
    {
        /// <summary>
        ///     Gets or sets the datetime when the event occured.
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        ///     Gets or sets the sequence number that is used to replay the messages in the right order.
        /// </summary>
        int Sequence { get; set; }

        /// <summary>
        ///     Gets or sets the serialized event.
        /// </summary>
        string SerializedEvent { get; set; }

        /// <summary>
        ///     Gets or sets the assembly qualified name of the event class.
        /// </summary>
        string? ClrType { get; set; }
    }
}
