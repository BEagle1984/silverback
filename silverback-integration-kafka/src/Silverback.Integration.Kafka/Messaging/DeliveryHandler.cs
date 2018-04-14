using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    public class DeliveryHandler : IDeliveryHandler<byte[], byte[]>
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly Func<IDeliveryResultHandler> _handler;

        /// <inheritdoc />
        public DeliveryHandler(Func<IDeliveryResultHandler> handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public void HandleDeliveryReport(Message<byte[], byte[]> message)
        {
            using (var processor = _handler.Invoke())
            {
                processor.ProcessDelivery(message);
            }
        }

        /// <inheritdoc />
        public bool MarshalData { get; }
    }
}
