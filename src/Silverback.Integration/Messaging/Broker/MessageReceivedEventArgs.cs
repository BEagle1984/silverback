// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(IRawInboundEnvelope envelope)
        {
            Envelope = envelope;
        }

        public IRawInboundEnvelope Envelope { get; }
    }
}