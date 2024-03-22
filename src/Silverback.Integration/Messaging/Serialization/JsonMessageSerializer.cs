// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
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
    public sealed class JsonMessageSerializer : JsonMessageSerializerBase, IEquatable<JsonMessageSerializer>
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="JsonMessageSerializer" />.
        /// </summary>
        public static JsonMessageSerializer Default { get; } = new();

        /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
        public override bool RequireHeaders => true;

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "CA2000", Justification = "MemoryStream is being returned")]
        public override ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            if (message == null)
                return ValueTaskFactory.FromResult<Stream?>(null);

            if (message is Stream inputStream)
                return ValueTaskFactory.FromResult<Stream?>(inputStream);

            if (message is byte[] inputBytes)
                return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(inputBytes));

            var type = message.GetType();

            messageHeaders.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

            var bytes = JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
            return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(bytes));
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public override async ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders);

            if (messageStream == null)
                return (null, type ?? typeof(object));

            if (messageStream.CanSeek && messageStream.Length == 0)
                return (null, type ?? typeof(object));

            if (type == null)
                throw new MessageSerializerException("Missing type header.");

            var deserializedObject = await JsonSerializer.DeserializeAsync(messageStream, type, Options, cancellationToken)
                                         .ConfigureAwait(false) ??
                                     throw new MessageSerializerException(
                                         "The deserialization returned null.");

            return (deserializedObject, type);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(JsonMessageSerializer? other) => ComparisonHelper.JsonEquals(this, other);

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((JsonMessageSerializer)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => 1;
    }
}
