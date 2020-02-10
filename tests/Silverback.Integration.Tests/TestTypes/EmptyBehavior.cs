// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class EmptyBehavior : IConsumerBehavior, IProducerBehavior
    {
        Task IConsumerBehavior.Handle(RawBrokerEnvelope envelope, RawBrokerMessageHandler next) => next(envelope);
        Task IProducerBehavior.Handle(RawBrokerEnvelope envelope, RawBrokerMessageHandler next) => next(envelope);
    }
}