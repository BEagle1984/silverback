using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Messaging
{
    /// <summary>
    /// Delivery Report Handler
    /// </summary>
    public abstract class DeliveryReportHandler : IDeliveryReportHandler
    {
        
        /// <inheritdoc />
        public abstract void HandleDeliveryReport(Message<byte[], byte[]> message);

        /// <inheritdoc />
        public bool MarshalData { get; }
    }
}
