// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class RawInboundMessage : InboundMessage<byte[]>, IRawInboundMessage
    {
        public RawInboundMessage(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset, IEndpoint endpoint, bool mustUnwrap) 
            : base(message, headers, offset, endpoint, mustUnwrap)
        {
        }
    }
}