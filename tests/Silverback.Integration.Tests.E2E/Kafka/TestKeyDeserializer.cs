// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.Kafka;

/// <summary>
/// Test implementation that transforms key value (appends an x character) after reading from UTF8 bytes.
/// </summary>
internal class TestKeyDeserializer : IMessageKeyDeserializer
{
    public ValueTask<string?> DeserializeAsync(Stream? keyStream, MessageHeaderCollection headers, ConsumerEndpoint endpoint)
    {
#pragma warning disable CA1849 // Intentionally synchronous for zero-allocation ValueTask completion
        byte[]? bytes = keyStream.ReadAll();
#pragma warning restore CA1849

        if (bytes == null || bytes.Length == 0)
            return new ValueTask<string?>((string?)null);

        string key = Encoding.UTF8.GetString(bytes);
        string extendedKey = $"{key}x";
        return new ValueTask<string?>(extendedKey);
    }
}
