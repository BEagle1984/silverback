// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages sent through the broker.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        ///     Gets a value indicating whether headers are mandatory for this serializer implementation or configuration
        ///     to work properly.
        /// </summary>
        bool RequireHeaders { get; }

        /// <summary>
        ///     Serializes the specified message object into a byte array.
        /// </summary>
        /// <param name="message">
        ///     The message object to be serialized.
        /// </param>
        /// <param name="messageHeaders">
        ///     The message headers collection.
        /// </param>
        /// <param name="context">
        ///     The context information.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="Stream" /> with the serialized message.
        /// </returns>
        ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);

        /// <summary>
        ///     Deserializes the byte array back into a message object.
        /// </summary>
        /// <param name="messageStream">
        ///     The <see cref="Stream" /> containing the message to be deserialized.
        /// </param>
        /// <param name="messageHeaders">
        ///     The message headers collection.
        /// </param>
        /// <param name="context">
        ///     The context information.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     deserialized message (or <c>null</c> when the input is null or empty) and the type of the message.
        /// </returns>
        ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);
    }
}
