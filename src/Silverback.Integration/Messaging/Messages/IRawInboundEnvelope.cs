// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IRawInboundEnvelope : IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the source endpoint.
        /// </summary>
        new IConsumerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets the name of the actual source endpoint (in case the <code>Endpoint</code> configuration
        ///     points to multiple endpoints, for example if consuming multiple topics with a single
        ///     <code>KafkaConsumer</code>).
        /// </summary>
        string ActualEndpointName { get; }
    }
}