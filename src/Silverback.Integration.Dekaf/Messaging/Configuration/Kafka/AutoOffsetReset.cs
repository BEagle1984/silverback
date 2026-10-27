// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The action to take when there is no initial offset in the offset store or the desired offset is out of range.
/// </summary>
public enum AutoOffsetReset
{
    /// <summary>
    ///     Automatically reset to the largest offset.
    /// </summary>
    Latest,

    /// <summary>
    ///     Automatically reset to the smallest offset.
    /// </summary>
    Earliest,

    /// <summary>
    ///     Throw an exception.
    /// </summary>
    Error
}
