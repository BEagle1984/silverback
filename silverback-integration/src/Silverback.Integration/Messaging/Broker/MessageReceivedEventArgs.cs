// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(IMessage message, IOffset offset)
        {
            Message = message;
            Offset = offset;
        }

        public IMessage Message { get; set; }

        public IOffset Offset { get; set; }
    }
}