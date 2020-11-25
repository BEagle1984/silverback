// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Wraps the message that is being produced to an outbound endpoint.
    /// </summary>
    public interface IRawOutboundEnvelope : IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the destination endpoint.
        /// </summary>
        new IProducerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets the message identifier on the message broker (the Kafka offset or similar).
        /// </summary>
        /// <remarks>
        ///     The identifier value will be set only after the message has been successfully published to the message
        ///     broker.
        /// </remarks>
        IBrokerMessageIdentifier? BrokerMessageIdentifier { get; }
    }
}
