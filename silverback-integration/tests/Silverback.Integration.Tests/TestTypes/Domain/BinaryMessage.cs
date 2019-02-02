using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Tests.TestTypes.Domain
{
    public class BinaryMessage
    {
        public Guid MessageId { get; set; }
        public byte[] Content { get; set; }
    }
}
