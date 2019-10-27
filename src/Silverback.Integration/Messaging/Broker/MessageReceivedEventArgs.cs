// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset,
            IEndpoint endpoint)
        {
            Message = message;
            Headers = headers;
            Offset = offset;
            Endpoint = endpoint;
        }

        public byte[] Message { get; }
        public IEnumerable<MessageHeader> Headers { get; }
        public IOffset Offset { get; }
        public IEndpoint Endpoint { get; }
    }
}