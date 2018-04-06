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
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> to be used.
        /// </summary>
        /// <returns></returns>
        IBroker GetBroker();

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Broker.IBroker" /> to be used casting it to the specified type.
        /// </summary>
        /// <typeparam name="TBroker">The type of the broker.</typeparam>
        /// <returns></returns>
        TBroker GetBroker<TBroker>() where TBroker : IBroker;

        /// <summary>
        /// Gets an <see cref="IProducer"/> instance to produce messages to this endpoint.
        /// </summary>
        /// <returns></returns>
        IProducer GetProducer();

        /// <summary>
        /// Gets an <see cref="IConsumer"/> instance to consume messages from this enpoint.
        /// </summary>
        /// <returns></returns>
        IConsumer GetConsumer();
    }
}