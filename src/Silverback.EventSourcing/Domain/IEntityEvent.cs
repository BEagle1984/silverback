// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Domain;

/// <summary>
///     A recorded event that can be re-applied to rebuild the entity status.
/// </summary>
public interface IEntityEvent
{
    /// <summary>
    ///     Gets or sets the datetime when the event occured.
    /// </summary>
    DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the sequence number that is used to replay the messages in the right order.
    /// </summary>
    int Sequence { get; set; }
}
