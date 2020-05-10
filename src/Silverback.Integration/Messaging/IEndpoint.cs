// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint to connect to (such as a Kafka topic or RabbitMQ queue or exchange).
    /// </summary>
    public interface IEndpoint
    {
        /// <summary>
        ///     Gets a string identifying the endpoint (the topic, queue or exchange name).
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the <see cref="IMessageSerializer" /> to be used to serialize or deserialize
        ///     the messages being produced or consumed.
        /// </summary>
        IMessageSerializer Serializer { get; }

        /// <summary>
        ///     <para>
        ///         Gets the encryption settings. This optional settings enables the end-to-end message encryption.
        ///     </para>
        ///     <para>
        ///         When enabled the messages are transparently encrypted by the producer and decrypted by the consumer.
        ///     </para>
        ///     <para>
        ///         Set it to <c>null</c> (default) to disable this feature.
        ///     </para>
        /// </summary>
        EncryptionSettings? Encryption { get; }

        /// <summary>
        ///     Validates the endpoint configuration and throws an <see cref="EndpointConfigurationException" />
        ///     if not valid.
        /// </summary>
        void Validate();
    }
}