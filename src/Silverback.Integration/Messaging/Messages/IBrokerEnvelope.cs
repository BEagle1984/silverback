// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the deserialized inbound or outbound message.
/// </summary>
public interface IBrokerEnvelope : IRawBrokerEnvelope, IEnvelope
{
    /// <summary>
    ///     Gets a value indicating whether the message is a tombstone (i.e. it's just a marker to indicate that the message with the same
    ///     key has been deleted).
    /// </summary>
    /// <returns>
    ///     A value indicating whether the message is a tombstone.
    /// </returns>
    bool IsTombstone { get; }
}
