using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    public class InboundMessageUnwrapper : ISubscriber
    {
        [Subscribe]
        public object OnMessageReceived(IInboundMessage message) => message.MustUnwrap ? message.Message : null;
    }
}
