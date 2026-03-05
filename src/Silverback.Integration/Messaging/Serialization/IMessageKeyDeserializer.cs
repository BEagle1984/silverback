// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the message key (used by broker technologies that support message keys, e.g., Kafka).
/// </summary>
public interface IMessageKeyDeserializer
{
    /// <summary>
    ///     Deserializes the raw key bytes into a string representation.
    /// </summary>
    /// <param name="keyStream">
    ///     The <see cref="Stream" /> containing the raw key bytes, or <c>null</c> if the message has no key.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The consumer endpoint configuration.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     deserialized key string, or <c>null</c> if the key is empty or not present.
    /// </returns>
    ValueTask<string?> DeserializeAsync(Stream? keyStream, MessageHeaderCollection headers, ConsumerEndpoint endpoint);
}
