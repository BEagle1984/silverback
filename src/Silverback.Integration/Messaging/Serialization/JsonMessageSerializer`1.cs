// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public class JsonMessageSerializer<TMessage> : JsonMessageSerializer
    {
        private readonly Type _type = typeof(TMessage);

        /// <inheritdoc cref="JsonMessageSerializer.Serialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null)
                return null;

            if (message is byte[] bytes)
                return bytes;

            return JsonSerializer.SerializeToUtf8Bytes(message, _type, Options);
        }

        /// <inheritdoc cref="JsonMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null || message.Length == 0)
                return (null, _type);

            var deserializedObject = JsonSerializer.Deserialize(message, _type, Options);
            return (deserializedObject, _type);
        }
    }
}
