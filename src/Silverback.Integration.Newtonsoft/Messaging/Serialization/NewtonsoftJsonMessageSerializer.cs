// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
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
        public static NewtonsoftJsonMessageSerializer Default { get; } = new();

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "CA2000", Justification = "MemoryStream is being returned")]
        public override ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            if (message == null)
                return ValueTaskFactory.FromResult<Stream?>(null);

            if (message is Stream inputStream)
                return ValueTaskFactory.FromResult<Stream?>(inputStream);

            if (message is byte[] inputBytes)
                return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(inputBytes));

            var type = message.GetType();
            var jsonString = JsonConvert.SerializeObject(message, type, Settings);

            messageHeaders.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

            return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(SystemEncoding.GetBytes(jsonString)));
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public override async ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders) ??
                       throw new InvalidOperationException("Message type is null.");

            if (messageStream == null)
                return (null, type);

            var buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);
            var jsonString = SystemEncoding.GetString(buffer!);

            var deserializedObject = JsonConvert.DeserializeObject(jsonString, type, Settings);

            return (deserializedObject, type);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(NewtonsoftJsonMessageSerializer? other) =>
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

            return Equals((NewtonsoftJsonMessageSerializer)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => 0;
    }
}
