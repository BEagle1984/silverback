// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(RawBrokerEnvelope envelope)
        {
            Envelope = envelope;
        }

        public RawBrokerEnvelope Envelope { get; }
    }
}