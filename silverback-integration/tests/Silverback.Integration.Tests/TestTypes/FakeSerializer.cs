// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public class FakeSerializer : IMessageSerializer
    {
        public byte[] Serialize(IMessage message)
        {
            throw new NotImplementedException();
        }

        public IMessage Deserialize(byte[] message)
        {
            throw new NotImplementedException();
        }
    }
}
