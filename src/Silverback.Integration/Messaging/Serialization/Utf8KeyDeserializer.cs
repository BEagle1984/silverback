// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the message key as a UTF-8 encoded string. This is the default
///     key deserializer and replicates the standard key handling behavior.
/// </summary>
public class Utf8KeyDeserializer : IMessageKeyDeserializer
{
    /// <summary>
    ///     Gets a shared default instance of the <see cref="Utf8KeyDeserializer" />.
    /// </summary>
    public static Utf8KeyDeserializer Default { get; } = new();

    /// <inheritdoc />
    public ValueTask<string?> DeserializeAsync(Stream? keyStream, MessageHeaderCollection headers, ConsumerEndpoint endpoint)
    {
#pragma warning disable CA1849 // Intentionally synchronous for zero-allocation ValueTask completion
        byte[]? bytes = keyStream.ReadAll();
#pragma warning restore CA1849

        if (bytes == null || bytes.Length == 0)
            return new ValueTask<string?>((string?)null);

        return new ValueTask<string?>(Encoding.UTF8.GetString(bytes));
    }
}
