using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// Apache Kafka broker class for Silverback Messaging bus
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Broker" />
    /// <inheritdoc />
    public class KafkaBroker : Broker
    {
        /// <inheritdoc cref="Broker.GetNewProducer"/>
        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.Producer" /> instance to publish messages to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public override Producer GetNewProducer(IEndpoint endpoint)
            => new KafkaProducer(this, (KafkaEndpoint)endpoint);

        /// <inheritdoc cref="Broker.GetNewConsumer"/>
        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.Consumer" /> instance to listen to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public override Consumer GetNewConsumer(IEndpoint endpoint)
            => new KafkaConsumer(this, (KafkaEndpoint)endpoint);

        /// <inheritdoc cref="Broker.Connect(System.Collections.Generic.IEnumerable{Silverback.Messaging.Broker.IConsumer})"/>
        /// <summary>
        /// Connects the specified consumers.
        /// After a successful call to this method the consumers will start listening to
        /// their endpoints.
        /// </summary>
        /// <param name="consumers">The consumers.</param>
        protected override void Connect(IEnumerable<IConsumer> consumers) 
            => consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Connect());

        ///<inheritdoc cref="Broker.Connect(System.Collections.Generic.IEnumerable{Silverback.Messaging.Broker.IProducer})"/>
        /// <summary>
        /// Connects the specified producers.
        /// After a successful call to this method the producers will be ready to send messages.
        /// </summary>
        /// <param name="producers">The producers.</param>
        protected override void Connect(IEnumerable<IProducer> producers)
            => producers.Cast<KafkaProducer>().ToList().ForEach(c => c.Connect());
        
        /// <inheritdoc cref="Broker.Disconnect(System.Collections.Generic.IEnumerable{Silverback.Messaging.Broker.IConsumer})"/>
        /// <summary>
        /// Disconnects the specified consumers. The consumers will not receive any further
        /// message.
        /// their endpoints.
        /// </summary>
        /// <param name="consumers">The consumers.</param>
        protected override void Disconnect(IEnumerable<IConsumer> consumers)
            => consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Disconnect());
        
        /// <inheritdoc cref="Broker.Disconnect(System.Collections.Generic.IEnumerable{Silverback.Messaging.Broker.IProducer})"/>
        /// <summary>
        /// Disconnects the specified producers. The producers will not be able to send messages anymore.
        /// </summary>
        /// <param name="producer">The producer.</param>
        protected override void Disconnect(IEnumerable<IProducer> producer)
            => producer.Cast<KafkaProducer>().ToList().ForEach(c => c.Disconnect());
    }
}
