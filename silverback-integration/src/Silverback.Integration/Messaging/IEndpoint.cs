using System;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging
{
    /// <summary>
    /// Contains information to identify the endpoint on the message broker (server address, topic, queue name, ...).
    /// </summary>
    public interface IEndpoint
    {
        /// <summary>
        /// Gets or sets the topic/queue name.
        /// </summary>
        string Name { get; }
    }
}