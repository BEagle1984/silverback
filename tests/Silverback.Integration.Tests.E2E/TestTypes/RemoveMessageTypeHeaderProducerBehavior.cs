// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes;

public class RemoveMessageTypeHeaderProducerBehavior : IProducerBehavior
{
    public int SortIndex => int.MaxValue;

    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        context.Envelope.Headers.Remove(DefaultMessageHeaders.MessageType);

        return next(context, cancellationToken);
    }
}
