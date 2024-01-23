// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal abstract record RawBrokerEnvelope : IRawBrokerEnvelope
{
    protected RawBrokerEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        Endpoint endpoint)
    {
        RawMessage = rawMessage;
        Headers = new MessageHeaderCollection(headers);
        Endpoint = Check.NotNull(endpoint, nameof(endpoint));
    }

    public MessageHeaderCollection Headers { get; init; }

    public Endpoint Endpoint { get; }

    public Stream? RawMessage { get; set; }
}
