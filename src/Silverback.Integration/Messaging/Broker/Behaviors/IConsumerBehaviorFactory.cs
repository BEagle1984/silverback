// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build an instance of the actual <see cref="IConsumerBehaviorFactory" /> per each
    ///     <see cref="IConsumer" /> that gets instantiated.
    /// </summary>
    public interface IConsumerBehaviorFactory : IBrokerBehaviorFactory<IConsumerBehavior>
    {
    }
}