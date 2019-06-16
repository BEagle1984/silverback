// Copyright (c) 2018-2019 Sergio Aquilini
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

        public byte[] Message { get; private set; }

        public IEnumerable<MessageHeader> Headers { get; private set; }
    }
}