// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a message broker endpoint to connect to (such as a Kafka topic or RabbitMQ queue or
    ///     exchange).
    /// </summary>
    public interface IEndpoint
    {
        /// <summary>
        ///     Gets a string identifying the endpoint (the topic, queue or exchange name).
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        ///     Gets the <see cref="IMessageSerializer" /> to be used to serialize or deserialize the messages being
        ///     produced or consumed.
        /// </summary>
        IMessageSerializer Serializer { get; }

        /// <summary>
        ///     <para>
        ///         Gets the encryption settings. This optional settings enables the end-to-end message encryption.
        ///     </para>
        ///     <para>
        ///         When enabled the messages are transparently encrypted by the producer and decrypted by the
        ///         consumer.
        ///     </para>
        ///     <para>
        ///         Set it to <c>null</c> (default) to disable this feature.
        ///     </para>
        /// </summary>
        EncryptionSettings? Encryption { get; }

        /// <summary>
        ///     Gets an optional friendly name to be used to identify the endpoint. This name can be used to
        ///     filter or retrieve the endpoints and will also be included in the <see cref="IEndpoint.DisplayName" />,
        ///     to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
        /// </summary>
        string? FriendlyName { get; }

        /// <summary>
        ///     Gets the message validation mode. This option can be used to specify if the messages have to be validated
        ///     and whether an exception must be thrown if the message is not valid.
        /// </summary>
        MessageValidationMode MessageValidationMode { get; }

        /// <summary>
        ///     Validates the endpoint configuration and throws an <see cref="EndpointConfigurationException" /> if
        ///     not valid.
        /// </summary>
        void Validate();
    }
}
