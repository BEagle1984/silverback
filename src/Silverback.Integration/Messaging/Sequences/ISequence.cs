// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Represents a set of related messages.
    /// </summary>
    public interface ISequence
    {
        /// <summary>
        ///     Gets the identifier that is used to match the consumed messages with their belonging sequence.
        /// </summary>
        object SequenceId { get; }

        /// <summary>
        ///     Gets the length of the sequence so far.
        /// </summary>
        int Length { get; }

        /// <summary>
        ///     Gets the declared total length of the sequence, if known.
        /// </summary>
        int? TotalLength { get; }

        /// <summary>
        ///     Gets the offsets of the messages belonging to the sequence.
        /// </summary>
        IReadOnlyList<IOffset> Offsets { get; }

        // TODO: ISequence.IsCompleted (and IMessageStream.IsCompleted) may be useful?

        /// <summary>
        ///     Gets a stream that will be pushed with the messages belonging to the sequence.
        /// </summary>
        IMessageStreamEnumerable<IRawInboundEnvelope> Stream { get; }
    }
}
