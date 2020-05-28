// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     The default implementation of a <see cref="IKafkaMessageSerializer" /> simply uses the provided
    ///     <see cref="IMessageSerializer" /> for the value and treats the key as a UTF-8 encoded string.
    /// </summary>
    public class DefaultKafkaMessageSerializer : IKafkaMessageSerializer
    {
        private readonly IMessageSerializer _serializer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultKafkaMessageSerializer" /> class.
        /// </summary>
        /// <param name="serializer">
        ///     The <see cref="IMessageSerializer" /> to be used.
        /// </param>
        public DefaultKafkaMessageSerializer(IMessageSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.Serialize(message, messageHeaders, context);

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.Deserialize(message, messageHeaders, context);

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task<byte[]?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.SerializeAsync(message, messageHeaders, context);

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task<(object?, Type)> DeserializeAsync(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            _serializer.DeserializeAsync(message, messageHeaders, context);

        /// <inheritdoc />
        public byte[] SerializeKey(
            string key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Encoding.UTF8.GetBytes(key);

        /// <inheritdoc />
        public string DeserializeKey(
            byte[] key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Encoding.UTF8.GetString(key);
    }
}
