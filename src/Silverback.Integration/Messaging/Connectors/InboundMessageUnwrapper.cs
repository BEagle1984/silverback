// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
