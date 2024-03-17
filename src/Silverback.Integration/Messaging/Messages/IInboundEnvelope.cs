// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawInboundEnvelope" />
public interface IInboundEnvelope : IBrokerEnvelope, IRawInboundEnvelope
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
