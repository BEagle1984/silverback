// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Silverback.Messaging.Messages;

internal abstract record RawBrokerEnvelope : IRawBrokerEnvelope
{
    protected RawBrokerEnvelope(Stream? rawMessage, IReadOnlyCollection<MessageHeader>? headers)
    {
        RawMessage = rawMessage;
        Headers = new MessageHeaderCollection(headers);
    }

    public MessageHeaderCollection Headers { get; init; }

    public Stream? RawMessage { get; set; }

    public string? GetMessageId() => Headers.GetValue(DefaultMessageHeaders.MessageId);

    // We abuse records for their cloning ability, but we revert to the reference equality because they are not fully immutable
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    // We abuse records for their cloning ability, but we revert to the reference equality because they are not fully immutable
    public virtual bool Equals(RawBrokerEnvelope? other) => ReferenceEquals(this, other);
}
