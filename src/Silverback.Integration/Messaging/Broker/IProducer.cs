// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Produces to an endpoint.
    /// </summary>
    public interface IProducer
    {
        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this .
        /// </summary>
        IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IProducerEndpoint" /> this instance is connected to.
        /// </summary>
        IProducerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets the collection of <see cref="IProducerBehavior" /> configured for this <see cref="IProducer" />.
        /// </summary>
        IReadOnlyCollection<IProducerBehavior> Behaviors { get; }

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="message">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="disableBehaviors">
        ///     When set to <c>true</c> the behaviors are not triggered and the message is just produced as-is.
        /// </param>
        void Produce(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null,
            bool disableBehaviors = false);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be delivered.
        /// </param>
        /// <param name="disableBehaviors">
        ///     When set to <c>true</c> the behaviors are not triggered and the message is just produced as-is.
        /// </param>
        void Produce(IOutboundEnvelope envelope, bool disableBehaviors = false);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="message">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="disableBehaviors">
        ///     When set to <c>true</c> the behaviors are not triggered and the message is just produced as-is.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ProduceAsync(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null,
            bool disableBehaviors = false);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be delivered.
        /// </param>
        /// <param name="disableBehaviors">
        ///     When set to <c>true</c> the behaviors are not triggered and the message is just produced as-is.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ProduceAsync(IOutboundEnvelope envelope, bool disableBehaviors = false);
    }
}
