// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestSerializer : IMessageSerializer
    {
        public int MustFailCount { get; set; }

        public int FailCount { get; private set; }

        public byte[] Serialize(object message, MessageHeaderCollection messageHeaders)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
        {
            var deserialized = new JsonMessageSerializer().Deserialize(message, messageHeaders);

            if (MustFailCount > FailCount)
            {
                FailCount++;
                throw new Exception("Test failure");
            }

            return deserialized;
        }

        public virtual Task<byte[]> SerializeAsync(object message, MessageHeaderCollection messageHeaders) =>
            Task.FromResult(Serialize(message, messageHeaders));

        public virtual Task<object> DeserializeAsync(byte[] message, MessageHeaderCollection messageHeaders) =>
            Task.FromResult(Deserialize(message, messageHeaders));
    }
}