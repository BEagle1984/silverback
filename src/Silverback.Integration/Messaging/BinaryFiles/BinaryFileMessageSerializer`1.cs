// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Handles the <see cref="IBinaryFileMessage" />. It's not really a serializer, since the raw binary
    ///     content is transmitted as-is.
    /// </summary>
    /// <typeparam name="TModel">
    ///     The type of the <see cref="IBinaryFileMessage" /> implementation.
    /// </typeparam>
    public class BinaryFileMessageSerializer<TModel> : IMessageSerializer
        where TModel : IBinaryFileMessage, new()
    {
        private readonly Type _type = typeof(TModel);

        /// <inheritdoc cref="BinaryFileMessageSerializer.SerializeAsync" />
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

            return ValueTaskFactory.FromResult(binaryFileMessage.Content);
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public ValueTask<(object?, Type)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            var binaryFileMessage = new TModel { Content = messageStream };

            return ValueTaskFactory.FromResult<(object?, Type)>((binaryFileMessage, _type));
        }
    }
}
