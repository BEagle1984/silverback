using System;
using System.Text;

namespace Silverback.Messaging.Broker
{
    /// <summary>Can represent a Kafka offset or other similar constructs such as delivery-tag in AMQP.</summary>
    public interface IOffset : IComparable
    {
        /// <summary>The unique key of the queue/topic/partition this offset belongs to.</summary>
        string Key { get; }

        string Value { get; }
    }
}
