// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.TestTypes
{
    public class FakeSerializer : IMessageSerializer
    {
        public byte[] Serialize(object message)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(byte[] message)
        {
            throw new NotImplementedException();
        }
    }
}
