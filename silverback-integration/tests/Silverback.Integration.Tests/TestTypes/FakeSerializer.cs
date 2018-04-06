using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.TestTypes
{
    public class FakeSerializer : IMessageSerializer
    {
        public byte[] Serialize(IEnvelope envelope)
        {
            throw new NotImplementedException();
        }

        public IEnvelope Deserialize(byte[] message)
        {
            throw new NotImplementedException();
        }
    }
}
