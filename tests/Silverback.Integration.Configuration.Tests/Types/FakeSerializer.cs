// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.Configuration.Types
{
    public class FakeSerializer : IMessageSerializer
    {
        public byte[] Serialize(object message)
        {
            throw new System.NotImplementedException();
        }

        public object Deserialize(byte[] message)
        {
            throw new System.NotImplementedException();
        }

        public FakeSerializerSettings Settings { get; set; } = new FakeSerializerSettings();
    }
}
