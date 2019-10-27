// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public interface IProducer
    {
        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be delivered.</param>
        /// <param name="headers">The optional message headers.</param>
        void Produce(object message, IEnumerable<MessageHeader> headers = null);

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be delivered.</param>
        /// <param name="headers">The optional message headers.</param>
        Task ProduceAsync(object message, IEnumerable<MessageHeader> headers = null);
    }
}