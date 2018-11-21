using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class TestErrorPolicy : ErrorPolicyBase
    {
        public bool Applied { get; private set; }

        protected override void ApplyPolicy(IMessage message, Action<IMessage> messageHandler, Exception exception)
        {
            Applied = true;
        }

        public TestErrorPolicy() : base(NullLoggerFactory.Instance.CreateLogger<TestErrorPolicy>())
        {
        }
    }
}