// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences.Unbounded
{
    /// <summary>
    ///     This isn't a real sequence but it's used to handle the stream pushed with all messages not actually
    ///     belonging to a sequence.
    /// </summary>
    internal class UnboundedSequence : Sequence
    {
        [SuppressMessage("", "CA2000", Justification = "Stream disposed in base class")]
        public UnboundedSequence(string sequenceId, ConsumerPipelineContext context)
            : base(
                sequenceId,
                context,
                false,
                streamProvider: new MessageStreamProvider<IInboundEnvelope>
                {
                    AllowSubscribeAsEnumerable = false
                })
        {
        }
    }
}
