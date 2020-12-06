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
    ///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public sealed class NewtonsoftJsonMessageSerializer<TMessage>
        : NewtonsoftJsonMessageSerializerBase, IEquatable<NewtonsoftJsonMessageSerializer<TMessage>>
    {
        private readonly Type _type = typeof(TMessage);

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "CA2000", Justification = "MemoryStream is being returned")]
        public override ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null)
                return ValueTaskFactory.FromResult<Stream?>(null);

            if (message is Stream inputStream)
                return ValueTaskFactory.FromResult<Stream?>(inputStream);

            if (message is byte[] inputBytes)
                return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(inputBytes));

            var jsonString = JsonConvert.SerializeObject(message, _type, Settings);

            return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(GetSystemEncoding().GetBytes(jsonString)));
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public override async ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (messageStream == null)
                return (null, _type);

            var buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);
            var jsonString = GetSystemEncoding().GetString(buffer!);

            var deserializedObject = JsonConvert.DeserializeObject(jsonString, _type, Settings);
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
