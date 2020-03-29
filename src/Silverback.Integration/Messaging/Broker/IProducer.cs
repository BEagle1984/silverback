// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public interface IProducer
    {
        /// <summary>
        ///     Gets the collection of <see cref="IProducerBehavior" /> configured for this <see cref="IProducer" />.
        /// </summary>
        IReadOnlyCollection<IProducerBehavior> Behaviors { get; }

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be delivered.</param>
        /// <param name="headers">The optional message headers.</param>
        void Produce(object message, IReadOnlyCollection<MessageHeader> headers = null);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be delivered.</param>
        /// <param name="headers">The optional message headers.</param>
        Task ProduceAsync(object message, IReadOnlyCollection<MessageHeader> headers = null);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be delivered.</param>
        void Produce(IOutboundEnvelope envelope);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be delivered.</param>
        Task ProduceAsync(IOutboundEnvelope envelope);
    }
}