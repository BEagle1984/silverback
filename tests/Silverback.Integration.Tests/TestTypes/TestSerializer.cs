// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes;

public class TestSerializer : IMessageSerializer
{
    public bool RequireHeaders => false;

    public int MustFailCount { get; set; }

    public int FailCount { get; private set; }

    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        throw new NotSupportedException();
    }

    public ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        if (MustFailCount > FailCount)
        {
            FailCount++;
            throw new InvalidOperationException("Test failure");
        }

        return DefaultSerializers.Json.DeserializeAsync(messageStream, headers, endpoint);
    }
}
