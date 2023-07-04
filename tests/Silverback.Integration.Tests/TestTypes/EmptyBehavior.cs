// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class EmptyBehavior : IConsumerBehavior, IProducerBehavior
    {
        public int SortIndex => 0;

        public Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next,
            CancellationToken cancellationToken = default) => next(context, cancellationToken);

        public Task HandleAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler next,
            CancellationToken cancellationToken = default) => next(context, cancellationToken);
    }
}
