// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestSerializer : IMessageSerializer
    {
        public bool RequireHeaders => false;

        public int MustFailCount { get; set; }

        public int FailCount { get; private set; }

        public ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            if (MustFailCount > FailCount)
            {
                FailCount++;
                throw new InvalidOperationException("Test failure");
            }

            return new JsonMessageSerializer().DeserializeAsync(messageStream, messageHeaders, context, cancellationToken);
        }
    }
}
