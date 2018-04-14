using System;
using Confluent.Kafka;

namespace Silverback.Messaging
{    
    /// <summary>
    /// Delivery Result Handler interface
    /// </summary>
    public interface IDeliveryResultHandler : IDisposable
    {
        /// <summary>
        /// Processes the message resulting from the delivery
        /// </summary>
        /// <param name="message"></param>
        void ProcessDelivery(Message<byte[], byte[]> message);
    }
}