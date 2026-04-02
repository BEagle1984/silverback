// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the message key as a UTF-8 encoded string. This is the default
///     key deserializer and replicates the standard key handling behavior.
/// </summary>
public class Utf8KeySerializer : IMessageKeySerializer
{
    /// <summary>
    ///     Gets a shared default instance of the <see cref="Utf8KeySerializer" />.
    /// </summary>
    public static Utf8KeySerializer Default { get; } = new();

    /// <inheritdoc />
    public ValueTask<Stream?> SerializeAsync(string? key, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        MemoryStream? stream = key != null ? new MemoryStream(Encoding.UTF8.GetBytes(key)) : null;
        return new ValueTask<Stream?>(stream);
    }
}
