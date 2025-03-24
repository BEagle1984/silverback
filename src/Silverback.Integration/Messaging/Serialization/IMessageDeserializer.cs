// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the messages consumed from broker.
/// </summary>
public interface IMessageDeserializer
{
    /// <summary>
    ///     Gets a value indicating whether this serializer (with the current configuration) needs the headers support to work properly.
    /// </summary>
    bool RequireHeaders { get; }

    /// <summary>
    ///     Deserializes the byte array into a message object.
    /// </summary>
    /// <param name="messageStream">
    ///     The <see cref="Stream" /> containing the message to be deserialized.
    /// </param>
    /// <param name="headers">
    ///     The message headers collection.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     deserialized message (or <c>null</c> when the input is null or empty) and the type of the message.
    /// </returns>
    ValueTask<DeserializedMessage> DeserializeAsync(Stream? messageStream, MessageHeaderCollection headers, ConsumerEndpoint endpoint);

    /// <summary>
    ///     Gets a new <see cref="IMessageSerializer" /> compatible with this deserializer.
    /// </summary>
    /// <returns>
    ///     A new instance of an <see cref="IMessageSerializer" /> compatible with this deserializer.
    /// </returns>
    IMessageSerializer GetCompatibleSerializer();
}
