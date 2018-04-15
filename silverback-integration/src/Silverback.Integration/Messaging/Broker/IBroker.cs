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
        /// Gets a new <see cref="IProducer" /> instance.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        IProducer GetProducer(IEndpoint endpoint);

        /// <summary>
        /// Gets a new <see cref="IConsumer" /> instance.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        IConsumer GetConsumer(IEndpoint endpoint);

        /// <summary>
        /// Initializes the connection to the specified endpoints.
        /// </summary>
        /// <remarks>This is called during the application startup and can be used to warm up the broker estabilishing 
        /// the connection and/or creating the consumers and producers beforehand.</remarks>
        /// <param name="inboundEndpoints">The inbound endpoints.</param>
        /// <param name="outboundEndpoints">The outbound endpoints.</param>
        void Connect(IEndpoint[] inboundEndpoints, IEndpoint[] outboundEndpoints);
    }
}
