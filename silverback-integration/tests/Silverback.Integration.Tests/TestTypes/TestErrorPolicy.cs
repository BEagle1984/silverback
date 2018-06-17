using System;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes
{
    public class TestErrorPolicy : ErrorPolicyBase
    {
        public bool Applied { get; private set; }

        protected override void ApplyPolicy<T>(T message, Action<T> handler)
        {
            Applied = true;
        }
    }
}