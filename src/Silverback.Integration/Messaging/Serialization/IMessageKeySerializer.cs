// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes the message key (used by broker technologies that support message keys, e.g., Kafka).
/// </summary>
public interface IMessageKeySerializer
{
    /// <summary>
    ///     Serializes the key value into a byte stream.
    /// </summary>
    /// <param name="key">
    ///     The key value to serialize, or <c>null</c> if no key is present.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The producer endpoint configuration.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="Stream" /> with the serialized key.
    /// </returns>
    ValueTask<Stream?> SerializeAsync(string? key, MessageHeaderCollection headers, ProducerEndpoint endpoint);
}
