// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestSerializer : IMessageSerializer
    {
        public int MustFailCount { get; set; }

        public int FailCount { get; private set; }

        public ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            throw new NotSupportedException();
        }

        public ValueTask<(object?, Type)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            if (MustFailCount > FailCount)
            {
                FailCount++;
                throw new InvalidOperationException("Test failure");
            }

            return new JsonMessageSerializer().DeserializeAsync(messageStream, messageHeaders, context);
        }
    }
}
