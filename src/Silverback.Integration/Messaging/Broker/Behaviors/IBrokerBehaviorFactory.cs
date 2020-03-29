// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build an instance of the actual <see cref="IBrokerBehavior" /> per each
    ///     <see cref="IProducer" /> or <see cref="IConsumer" /> that gets instantiated.
    /// </summary>
    /// <seealso cref="IProducerBehaviorFactory" />
    /// <seealso cref="IConsumerBehaviorFactory" />
    public interface IBrokerBehaviorFactory<TBehavior> : IBrokerBehavior
        where TBehavior : IBrokerBehavior
    {
        TBehavior Create();
    }
}