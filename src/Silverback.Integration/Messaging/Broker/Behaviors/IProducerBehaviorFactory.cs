// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build an instance of the actual <see cref="IProducerBehavior" /> per each
    ///     <see cref="IProducer" /> that gets instantiated.
    /// </summary>
    public interface IProducerBehaviorFactory : IBrokerBehaviorFactory<IProducerBehavior>
    {
    }
}
