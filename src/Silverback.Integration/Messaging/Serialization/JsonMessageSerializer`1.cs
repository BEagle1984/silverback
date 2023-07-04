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
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public sealed class JsonMessageSerializer<TMessage>
        : JsonMessageSerializerBase, IEquatable<JsonMessageSerializer<TMessage>>
    {
        private readonly Type _type = typeof(TMessage);

        /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
        public override bool RequireHeaders => false;

        /// <inheritdoc cref="JsonMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "CA2000", Justification = "MemoryStream is returend")]
        public override ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
                return ValueTaskFactory.FromResult<Stream?>(null);

            if (message is Stream inputStream)
                return ValueTaskFactory.FromResult<Stream?>(inputStream);

            if (message is byte[] inputBytes)
                return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(inputBytes));

            var bytes = JsonSerializer.SerializeToUtf8Bytes(message, _type, Options);
            return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(bytes));
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public override async ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            if (messageStream == null)
                return (null, _type);

            if (messageStream.CanSeek && messageStream.Length == 0)
                return (null, _type);

            var deserializedObject = await JsonSerializer.DeserializeAsync(messageStream, _type, Options, cancellationToken)
                                         .ConfigureAwait(false) ??
                                     throw new MessageSerializerException("The deserialization returned null.");

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
