// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryConsumer : Consumer<InMemoryBroker, IEndpoint>
    {
        public InMemoryConsumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger, MessageLogger messageLogger) : base(broker, endpoint, logger, messageLogger)
        {
        }

        public override void Acknowledge(IEnumerable<IOffset> offsets)
        {
            // Do nothing
        }

        internal void Receive(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset) =>
            HandleMessage(message, headers, offset);
    }
}