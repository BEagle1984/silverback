// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryConsumer : Consumer<InMemoryBroker, IEndpoint>
    {
        public InMemoryConsumer(IBroker broker, IEndpoint endpoint) : base(broker, endpoint)
        {
        }

        public override Task Acknowledge(IEnumerable<IOffset> offsets)
        {
            // Do nothing
            return Task.CompletedTask;
        }

        internal Task Receive(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset) =>
            HandleMessage(message, headers, offset);
    }
}