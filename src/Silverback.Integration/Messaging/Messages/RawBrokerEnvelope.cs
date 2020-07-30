// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    internal abstract class RawBrokerEnvelope : IRawBrokerEnvelope
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        protected RawBrokerEnvelope(
            byte[]? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IEndpoint endpoint,
            IOffset? offset,
            IDictionary<string, string>? additionalLogData)
        {
            RawMessage = rawMessage;
            Headers = new MessageHeaderCollection(headers);
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));
            Offset = offset;
            AdditionalLogData = additionalLogData ?? new Dictionary<string, string>();
        }

        public IEndpoint Endpoint { get; }

        public IDictionary<string, string> AdditionalLogData { get; }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? RawMessage { get; set; }

        public MessageHeaderCollection Headers { get; }

        public IOffset? Offset { get; internal set; }
    }
}
