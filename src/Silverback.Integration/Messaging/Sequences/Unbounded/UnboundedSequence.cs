// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences.Unbounded
{
    /// <summary>
    ///     This isn't a real sequence but it's used to handle the stream pushed with all messages not actually
    ///     belonging to a sequence.
    /// </summary>
    internal class UnboundedSequence : Sequence<IInboundEnvelope>
    {
        public UnboundedSequence(object sequenceId, ConsumerPipelineContext context)
            : base(sequenceId, context, false)
        {
        }
    }
}
