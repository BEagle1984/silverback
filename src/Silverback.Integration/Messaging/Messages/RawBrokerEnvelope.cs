// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    internal abstract class RawBrokerEnvelope : IRawBrokerEnvelope
    {
        protected RawBrokerEnvelope(
            Stream? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IEndpoint endpoint,
            IDictionary<string, string>? additionalLogData)
        {
            RawMessage = rawMessage;
            Headers = new MessageHeaderCollection(headers);
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));
            AdditionalLogData = additionalLogData ?? new Dictionary<string, string>();
        }

        public IEndpoint Endpoint { get; }

        public IDictionary<string, string> AdditionalLogData { get; }

        public MessageHeaderCollection Headers { get; }

        public Stream? RawMessage { get; set; }
    }
}
