// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class OutboundEnvelope : RawOutboundEnvelope, IOutboundEnvelope
    {
        private object? _message;

        public OutboundEnvelope(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            bool autoUnwrap = false,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null)
            : base(headers, endpoint, brokerMessageIdentifier)
        {
            _message = message;

            if (Message is byte[] rawMessage)
                RawMessage = new MemoryStream(rawMessage);

            if (Message is Stream stream)
                RawMessage = stream;

            AutoUnwrap = autoUnwrap;
        }

        public bool AutoUnwrap { get; }

        public object? Message
        {
            get => _message;
            set
            {
                _message = value;
                RawMessage = null;
            }
        }
    }
}
