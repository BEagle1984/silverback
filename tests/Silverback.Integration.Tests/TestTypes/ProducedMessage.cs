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
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ProducedMessage(byte[]? message, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
        {
            Message = message;

            if (headers != null)
                Headers.AddRange(headers);

            Endpoint = endpoint;
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[]? Message { get; }

        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        public IEndpoint Endpoint { get; }
    }
}
