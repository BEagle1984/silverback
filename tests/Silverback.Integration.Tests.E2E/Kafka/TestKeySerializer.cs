// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.E2E.Kafka;

/// <summary>
/// Test implementation that transforms key value (appends an x character) before getting UTF8 bytes.
/// </summary>
internal class TestKeySerializer : IMessageKeySerializer
{
    public ValueTask<Stream?> SerializeAsync(string? key, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        if (key == null)
        {
            return new ValueTask<Stream?>((Stream?)null);
        }

        string extendedKey = $"{key}x";
        return new ValueTask<Stream?>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(extendedKey)));
    }
}
