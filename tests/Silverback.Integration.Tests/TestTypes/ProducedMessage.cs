// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class ProducedMessage
    {
        public ProducedMessage(
            byte[]? message,
            IReadOnlyCollection<MessageHeader>? headers,
            IEndpoint endpoint)
        {
            Message = message;
            Headers = headers != null ? new MessageHeaderCollection(headers) : new MessageHeaderCollection();
            Endpoint = endpoint;
        }

        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[]? Message { get; }

        public MessageHeaderCollection Headers { get; }

        public IEndpoint Endpoint { get; }
    }
}
