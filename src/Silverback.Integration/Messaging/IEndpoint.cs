// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public interface IEndpoint
    {
        /// <summary>
        ///     Gets a string identifying the endpoint (being it a queue, topic, exchange, ...).
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets an instance of <see cref="IMessageSerializer" /> to be used to serialize or deserialize
        ///     the messages being produced or consumed.
        /// </summary>
        IMessageSerializer Serializer { get; }

        /// <summary>
        ///     Validates the endpoint configuration and throws an <see cref="EndpointConfigurationException" />
        ///     if not valid.
        /// </summary>
        /// <exception cref="EndpointConfigurationException"></exception>
        void Validate();
    }
}