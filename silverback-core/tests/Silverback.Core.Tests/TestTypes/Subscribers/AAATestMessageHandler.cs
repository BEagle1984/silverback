using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class AAATestMessageHandler : IMessageHandler
    {
        [Subscribe]
        public void OnTransactionCommit(TransactionCommitEvent message)
        {
            
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SubscribeAttribute : Attribute
    {
    }

    public interface IMessageHandler
    {
    }
}
