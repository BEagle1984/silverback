// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class EmptyBehavior : IConsumerBehavior, IProducerBehavior
    {
        public Task Handle(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next) => next(envelopes, serviceProvider, consumer);

        public Task Handle(IOutboundEnvelope envelope, IProducer producer, OutboundEnvelopeHandler next) =>
            next(envelope, producer);
    }
}