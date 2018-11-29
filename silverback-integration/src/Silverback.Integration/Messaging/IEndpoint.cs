using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public interface IEndpoint
    {
        /// <summary>
        /// Gets the topic/queue name.
        /// </summary>
        string Name { get; }

        IMessageSerializer Serializer { get; }
    }
}