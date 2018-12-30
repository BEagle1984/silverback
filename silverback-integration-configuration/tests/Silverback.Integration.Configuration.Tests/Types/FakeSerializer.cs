// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Types
{
    public class FakeSerializer : IMessageSerializer
    {
        public byte[] Serialize(IMessage message)
        {
            throw new System.NotImplementedException();
        }

        public IMessage Deserialize(byte[] message)
        {
            throw new System.NotImplementedException();
        }

        public FakeSerializerSettings Settings { get; set; } = new FakeSerializerSettings();
    }
}
