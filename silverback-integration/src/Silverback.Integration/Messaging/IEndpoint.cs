using Silverback.Messaging.Broker;

namespace Silverback.Messaging
{
    /// <summary>
    /// Contains information to identify the endpoint on the message broker (server address, topic, queue name, ...).
    /// </summary>
    public interface IEndpoint : IConfigurationValidation
    {
        /// <summary>
        /// Gets or sets the topic/queue name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the broker to be used.
        /// If not set the default one will be used.
        /// </summary>
        string BrokerName { get; set; }
    }
}