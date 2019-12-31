// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(RawBrokerMessage message)
        {
            Message = message;
        }

        public RawBrokerMessage Message { get; }
    }
}