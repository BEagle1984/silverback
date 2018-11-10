using Microsoft.Extensions.Logging;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The basic interface to interact with the message broker.
    /// </summary>
    public interface IBroker : IConfigurationValidation
    {
        /// <summary>
        /// Gets the configuration name. This is necessary only if working with multiple 
        /// brokers.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this configuration is the default one.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Gets the <see cref="IMessageSerializer"/> to be used to serialize and deserialize the messages
        /// being sent through the broker.
        /// </summary>
        IMessageSerializer GetSerializer();

        /// <summary>
        /// Gets an <see cref="IProducer" /> instance to publish messages to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        IProducer GetProducer(IEndpoint endpoint);

        /// <summary>
        /// Gets an <see cref="IConsumer" /> instance to listen to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        IConsumer GetConsumer(IEndpoint endpoint);

        /// <summary>
        /// Gets a value indicating whether this instance is connected with the message broker.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        bool IsConnected { get; }

        /// <summary>
        /// The logger factory
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Connects to the message broker.
        /// After a successful call to this method the created consumers will start listening to 
        /// their endpoints and the producers will be ready to send messages.
        /// </summary>
        void Connect();
        
        /// <summary>
        /// Disconnects from the message broker. The related consumers will not receive any further
        /// message and the producers will not be able to send messages anymore.
        /// </summary>
        void Disconnect();
    }
}
