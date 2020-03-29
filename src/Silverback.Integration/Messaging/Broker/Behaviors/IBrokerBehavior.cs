// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build a custom pipeline, plugging some functionality into either the <see cref="IProducer" />
    ///     (see <see cref="IProducerBehavior" />) or the <see cref="IConsumer" /> (see <see cref="IConsumerBehavior" />).
    /// </summary>
    public interface IBrokerBehavior
    {
    }
}