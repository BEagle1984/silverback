using System;
using Confluent.Kafka;

namespace Silverback.Messaging
{    
    /// <summary>
    /// Delivery Report Handler interface
    /// </summary>
    public interface IDeliveryReportHandler : IDeliveryHandler<byte[], byte[]>
    {
        
    }
}