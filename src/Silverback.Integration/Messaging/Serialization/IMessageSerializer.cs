// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes the messages produced to the broker.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    ///     Serializes the specified message object into a byte array.
    /// </summary>
    /// <param name="message">
    ///     The message object to be serialized.
    /// </param>
    /// <param name="headers">
    ///     The message headers collection.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="Stream" /> with the serialized message.
    /// </returns>
    ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint);
}
