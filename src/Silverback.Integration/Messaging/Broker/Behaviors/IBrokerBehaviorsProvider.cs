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
    public interface IBrokerBehaviorsProvider
    {
        /// <summary>
        ///     Creates a new <see cref="Stack{T}" /> of <see cref="IProducerBehavior" /> to be used in the
        ///     <see cref="IProducer" /> pipeline.
        /// </summary>
        /// <returns>The ready-to-use <see cref="Stack{T}" /> of <see cref="IProducerBehavior" />.</returns>
        Stack<IProducerBehavior> CreateProducerStack();

        /// <summary>
        ///     Creates a new <see cref="Stack{T}" /> of <see cref="IConsumerBehavior" /> to be used in the
        ///     <see cref="IConsumer" /> pipeline.
        /// </summary>
        /// <returns>The ready-to-use <see cref="Stack{T}" /> of <see cref="IConsumerBehavior" />.</returns>
        Stack<IConsumerBehavior> CreateConsumerStack();
    }
}
