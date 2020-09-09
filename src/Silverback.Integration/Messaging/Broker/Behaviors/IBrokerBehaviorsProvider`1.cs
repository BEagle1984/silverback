// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Provides the <see cref="Stack{T}" /> of <see cref="IProducerBehavior" /> and
    ///     <see cref="IConsumerBehavior" /> to be used in the <see cref="IProducer" /> and
    ///     <see cref="IConsumer" /> pipeline.
    /// </summary>
    /// <typeparam name="TBehavior">
    ///     The type of the behaviors to be provided, either <see cref="IProducerBehavior" /> or
    ///     <see cref="IConsumerBehavior" />.
    /// </typeparam>
    public interface IBrokerBehaviorsProvider<TBehavior>
        where TBehavior : IBrokerBehavior
    {
        /// <summary>
        ///     Creates a new <see cref="Stack{T}" /> of <see cref="IProducerBehavior" /> or
        ///     <see cref="IConsumerBehavior" /> to be used in the <see cref="IProducer" /> or
        ///     <see cref="IConsumer" /> pipeline.
        /// </summary>
        /// <returns>The ready-to-use <see cref="Stack{T}" /> of <typeparamref name="TBehavior" />.</returns>
        Stack<TBehavior> CreateStack();
    }
}
