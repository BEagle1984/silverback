// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build an instance of the actual <see cref="IBrokerBehavior" /> per each
    ///     <see cref="IProducer" /> or <see cref="IConsumer" /> that gets instantiated.
    /// </summary>
    /// <typeparam name="TBehavior">The type of the <see cref="IBrokerBehavior"/> being created.</typeparam>
    public interface IBrokerBehaviorFactory<TBehavior> : IBrokerBehavior
        where TBehavior : IBrokerBehavior
    {
        /// <summary>
        ///     Creates a new instance of the behavior that will be used for the execution of a single pipeline.
        /// </summary>
        /// <returns>A new instance of the behavior.</returns>
        TBehavior Create();
    }
}