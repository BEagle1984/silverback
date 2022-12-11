// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     Represent an incomplete sequence (missing the first message) and is used to signal the pipeline to ignore
///     the message.
/// </summary>
public class IncompleteSequence : RawSequence
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="IncompleteSequence" /> class.
    /// </summary>
    /// <param name="sequenceId">
    ///     The identifier that is used to match the consumed messages with their belonging sequence.
    /// </param>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
    ///     sequence gets published to the internal bus.
    /// </param>
    public IncompleteSequence(string sequenceId, ConsumerPipelineContext context)
        : base(sequenceId, context)
    {
    }
}
