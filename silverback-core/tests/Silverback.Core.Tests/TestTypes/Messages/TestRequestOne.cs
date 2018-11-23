using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestRequestOne : IRequest, ITestMessage
    {
        public Guid RequestId { get; set; }

        public string Message { get; set; }
    }
}
