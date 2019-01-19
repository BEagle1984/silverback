// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(object message, IOffset offset)
        {
            Message = message;
            Offset = offset;
        }

        public object Message { get; set; }

        public IOffset Offset { get; set; }
    }
}