// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.Configuration.Types
{
    public class FakeSerializer : IMessageSerializer
    {
        public FakeSerializerSettings Settings { get; set; } = new FakeSerializerSettings();

        public byte[] Serialize(object message, MessageHeaderCollection messageHeaders)
        {
            throw new System.NotImplementedException();
        }

        public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
        {
            throw new System.NotImplementedException();
        }
    }
}