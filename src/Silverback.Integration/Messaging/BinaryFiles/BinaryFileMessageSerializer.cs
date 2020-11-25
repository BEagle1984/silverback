// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Handles the default implementation of <see cref="IBinaryFileMessage" />. It's not really a
    ///     serializer, since the raw binary content is transmitted as-is.
    /// </summary>
    public class BinaryFileMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="BinaryFileMessageSerializer" />.
        /// </summary>
        public static BinaryFileMessageSerializer Default { get; } = new();

        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        [SuppressMessage("", "CA2000", Justification = "MemoryStream is being returned")]
        [SuppressMessage("", "ASYNC0002", Justification = "Async suffix is correct for ValueTask")]
        public ValueTask<Stream?> SerializeAsync(
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

            var binaryFileMessage = message as IBinaryFileMessage;
            if (binaryFileMessage == null)
            {
                throw new ArgumentException(
                    "The message is not implementing the IBinaryFileMessage interface.",
                    nameof(message));
            }

            messageHeaders.AddOrReplace(
                DefaultMessageHeaders.MessageType,
                message.GetType().AssemblyQualifiedName);

            return ValueTaskFactory.FromResult(binaryFileMessage.Content);
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        [SuppressMessage("", "ASYNC0002", Justification = "Async suffix is correct for ValueTask")]
        public ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(messageHeaders, nameof(messageHeaders));

            var type = SerializationHelper.GetTypeFromHeaders(messageHeaders, false) ?? typeof(BinaryFileMessage);

            var messageModel = (IBinaryFileMessage)Activator.CreateInstance(type);
            messageModel.Content = messageStream;

            return ValueTaskFactory.FromResult<(object?, Type)>((messageModel, type));
        }
    }
}
