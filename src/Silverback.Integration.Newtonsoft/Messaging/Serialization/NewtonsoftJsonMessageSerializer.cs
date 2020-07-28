// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes the messages in JSON format and relies on some added headers to determine the message
    ///     type upon deserialization. This default serializer is ideal when the producer and the consumer are
    ///     both using Silverback.
    /// </summary>
    public sealed class NewtonsoftJsonMessageSerializer
        : NewtonsoftJsonMessageSerializerBase, IEquatable<NewtonsoftJsonMessageSerializer>
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="NewtonsoftJsonMessageSerializer" />.
        /// </summary>
        public static NewtonsoftJsonMessageSerializer Default { get; } = new NewtonsoftJsonMessageSerializer();

        /// <inheritdoc cref="IMessageSerializer.Serialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override byte[]? Serialize(
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
            var json = JsonConvert.SerializeObject(message, type, Settings);

            messageHeaders.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

            return GetSystemEncoding().GetBytes(json);
        }

        /// <inheritdoc cref="IMessageSerializer.Deserialize" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public override (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders);

            if (message == null || message.Length == 0)
                return (null, type);

            var jsonString = GetSystemEncoding().GetString(message);

            var deserializedObject = JsonConvert.DeserializeObject(jsonString, type, Settings) ??
                                     throw new MessageSerializerException("The deserialization returned null.");

            return (deserializedObject, type);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(NewtonsoftJsonMessageSerializer? other) => NewtonsoftComparisonHelper.JsonEquals(this, other);

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((NewtonsoftJsonMessageSerializer)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => 0;
    }
}
