using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestRequestOne : IRequest
    {
        public Guid RequestId { get; set; }

        public string Message { get; set; }
    }
}
