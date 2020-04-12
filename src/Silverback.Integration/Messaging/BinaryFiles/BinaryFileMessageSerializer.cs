// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Handles the default implementation of <see cref="IBinaryFileMessage"/>. It's not really a serializer,
    ///     since the raw binary content is transmitted as-is.
    /// </summary>
    /// <inheritdoc cref="IMessageSerializer"/>
    public class BinaryFileMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Gets the default static instance of <see cref="BinaryFileMessageSerializer"/>.
        /// </summary>
        public static BinaryFileMessageSerializer Default { get; } = new BinaryFileMessageSerializer();

        public virtual byte[] Serialize(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            switch (message)
            {
                case null:
                    return null;
                case byte[] bytes:
                    return bytes;
            }

            var binaryFileMessage = message as IBinaryFileMessage;
            if (binaryFileMessage == null)
                throw new ArgumentException(
                    "The message is not implementing the IBinaryFileMessage interface.",
                    nameof(message));

            messageHeaders.AddOrReplace(
                DefaultMessageHeaders.MessageType,
                message.GetType().AssemblyQualifiedName);

            return binaryFileMessage.Content;
        }

        public virtual object Deserialize(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null || message.Length == 0)
                return null;

            var type = SerializationHelper.GetTypeFromHeaders<BinaryFileMessage>(messageHeaders);

            var messageModel = (IBinaryFileMessage) Activator.CreateInstance(type);
            messageModel.Content = message;

            return messageModel;
        }

        public Task<byte[]> SerializeAsync(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Serialize(message, messageHeaders, context));

        public Task<object> DeserializeAsync(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            Task.FromResult(Deserialize(message, messageHeaders, context));
    }

    /// <summary>
    ///     Handles the <see cref="IBinaryFileMessage"/>. It's not really a serializer, since the raw
    ///     binary content is transmitted as-is.
    /// </summary>
    /// <inheritdoc cref="BinaryFileMessageSerializer"/>
    /// <typeparam name="TModel">The type of the <see cref="IBinaryFileMessage"/> implementation.</typeparam>
    public class BinaryFileMessageSerializer<TModel> : BinaryFileMessageSerializer
        where TModel : IBinaryFileMessage, new()
    {
        public override byte[] Serialize(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            switch (message)
            {
                case null:
                    return null;
                case byte[] bytes:
                    return bytes;
            }

            var binaryFileMessage = message as IBinaryFileMessage;
            if (binaryFileMessage == null)
                throw new ArgumentException(
                    "The message is not implementing the IBinaryFileMessage interface.",
                    nameof(message));

            return binaryFileMessage.Content;
        }

        public override object Deserialize(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (message == null || message.Length == 0)
                return null;

            return new TModel { Content = message };
        }
    }
}