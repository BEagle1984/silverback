// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            Message = message;
            Headers = headers;
            Offset = offset;
        }

        public byte[] Message { get; set; }

        public IEnumerable<MessageHeader> Headers { get; set; }

        public IOffset Offset { get; set; }
    }
}