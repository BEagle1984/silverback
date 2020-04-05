// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class EmptyBehavior : IConsumerBehavior, IProducerBehavior
    {
        public Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next) => next(context, serviceProvider);

        public Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next) =>
            next(context);
    }
}