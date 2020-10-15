// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class EmptyBehavior : IConsumerBehavior, IProducerBehavior
    {
        public int SortIndex => 0;

        public Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next) => next(context);

        public Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next) => next(context);
    }
}
