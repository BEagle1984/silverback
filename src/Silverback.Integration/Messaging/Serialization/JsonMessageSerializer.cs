// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes the messages in JSON format and relies on some added headers to determine the message
    ///     type upon deserialization. This default serializer is ideal when the producer and the consumer are
    ///     both using Silverback.
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="JsonMessageSerializer" />.
        /// </summary>
        public static JsonMessageSerializer Default { get; } = new JsonMessageSerializer();

        /// <summary>
        ///     Gets or sets the options to be passed to the <see cref="JsonSerializer" />.
        /// </summary>
        public JsonSerializerOptions Options { get; set; } = new JsonSerializerOptions();

        /// <inheritdoc cref="IMessageSerializer.Serialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            if (message == null)
                return null;

            if (message is byte[] bytes)
                return bytes;

            var type = message.GetType();

            messageHeaders.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

            return JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
        }

        /// <inheritdoc cref="IMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders);

            if (message == null || message.Length == 0)
                return (null, type);

            var deserializedObject = JsonSerializer.Deserialize(message, type, Options) ??
                                     throw new MessageSerializerException("The deserialization returned null.");

            return (deserializedObject, type);
        }

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual Task<byte[]?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Serialize(message, messageHeaders, context));

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public virtual Task<(object?, Type)> DeserializeAsync(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Deserialize(message, messageHeaders, context));
    }
}
