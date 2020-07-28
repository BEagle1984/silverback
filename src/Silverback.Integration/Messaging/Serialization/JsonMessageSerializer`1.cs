// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public sealed class JsonMessageSerializer<TMessage>
        : JsonMessageSerializerBase, IEquatable<JsonMessageSerializer<TMessage>>
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

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(JsonMessageSerializer<TMessage>? other) => ComparisonHelper.JsonEquals(this, other);

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((JsonMessageSerializer<TMessage>?)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(1, typeof(TMessage).Name);
    }
}
