// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Silverback.Messaging;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class ProducedMessage
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ProducedMessage(Stream? message, IEnumerable<MessageHeader>? headers, IEndpoint endpoint)
        {
            Message = message;

            if (headers != null)
                Headers.AddRange(headers);

            Endpoint = endpoint;
        }

        public Stream? Message { get; }

        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        public IEndpoint Endpoint { get; }
    }
}
