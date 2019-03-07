// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.TestTypes
{
    public class WrappedInboundMessageSubscriber : ISubscriber
    {
        public List<IInboundMessage<object>> ReceivedMessages { get; } = new List<IInboundMessage<object>>();

        [Subscribe]
        void OnMessageReceived(IInboundMessage<object> message) => ReceivedMessages.Add(message);
    }
}