// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Wraps the message that is being consumed from an inbound endpoint.
    /// </summary>
    public interface IRawInboundEnvelope : IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the source endpoint.
        /// </summary>
        new IConsumerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets the name of the actual source endpoint (in case the <c>Endpoint</c> configuration points to
        ///     multiple endpoints, for example if consuming multiple topics with a
        ///     single <c>KafkaConsumer</c>).
        /// </summary>
        string ActualEndpointName { get; }

        /// <summary>
        ///     Gets the message offset (or similar construct if using a message broker other than Kafka).
        /// </summary>
        IOffset Offset { get; }
    }
}
