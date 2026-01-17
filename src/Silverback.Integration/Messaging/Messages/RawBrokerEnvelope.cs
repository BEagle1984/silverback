// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Silverback.Messaging.Messages;

internal abstract record RawBrokerEnvelope : IRawBrokerEnvelope
{
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Collection initializer doesn't work with nullable argument")]
    protected RawBrokerEnvelope(Stream? rawMessage, IReadOnlyCollection<MessageHeader>? headers)
    {
        RawMessage = rawMessage;
        Headers = headers == null ? [] : [.. headers];
    }

    public MessageHeaderCollection Headers { get; init; }

    public Stream? RawMessage { get; set; }

    // We abuse records for their cloning ability, but we revert to the reference equality because they are not fully immutable
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    // We abuse records for their cloning ability, but we revert to the reference equality because they are not fully immutable
    public virtual bool Equals(RawBrokerEnvelope? other) => ReferenceEquals(this, other);
}
