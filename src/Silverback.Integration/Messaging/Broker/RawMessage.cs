// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class RawMessage
    {
        public RawMessage(byte[] message, IEnumerable<MessageHeader> headers)
        {
            Message = message;
            Headers = headers;
        }

        public byte[] Message { get; }

        public IEnumerable<MessageHeader> Headers { get; }
    }
}