// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public sealed class NewtonsoftJsonMessageSerializer<TMessage>
        : NewtonsoftJsonMessageSerializerBase, IEquatable<NewtonsoftJsonMessageSerializer<TMessage>>
    {
        private readonly Type _type = typeof(TMessage);

        /// <inheritdoc cref="NewtonsoftJsonMessageSerializer.Serialize" />
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

            var jsonString = JsonConvert.SerializeObject(message, _type, Settings);

            return GetSystemEncoding().GetBytes(jsonString);
        }

        /// <inheritdoc cref="NewtonsoftJsonMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null || message.Length == 0)
                return (null, _type);

            var json = GetSystemEncoding().GetString(message);

            var deserializedObject = JsonConvert.DeserializeObject(json, _type, Settings);
            return (deserializedObject, _type);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(NewtonsoftJsonMessageSerializer<TMessage>? other) =>
            NewtonsoftComparisonHelper.JsonEquals(this, other);

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((NewtonsoftJsonMessageSerializer<TMessage>?)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(1, typeof(TMessage).Name);
    }
}
