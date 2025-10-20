// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Silverback.Messaging.Messages;

internal abstract record BrokerEnvelope : IBrokerEnvelope
{
    protected BrokerEnvelope()
    {
        Headers = [];
    }

    protected BrokerEnvelope(Stream? rawMessage)
    {
        RawMessage = rawMessage;
        Headers = [];
    }

    protected BrokerEnvelope(IBrokerEnvelope clonedEnvelope)
    {
        Message = clonedEnvelope.Message;
        RawMessage = clonedEnvelope.RawMessage;
        Headers = new MessageHeaderCollection(clonedEnvelope.Headers);
    }

    public abstract Type MessageType { get; }

    public bool IsTombstone => Message is null or ITombstone;

    public MessageHeaderCollection Headers { get; protected init; }

    public Stream? RawMessage { get; protected set; }

    public object? Message { get; protected init; }

    // We abuse records for their cloning ability, but we revert to the reference equality because they are not fully immutable
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    // We abuse records for their cloning ability, but we revert to the reference equality because they are not fully immutable
    public virtual bool Equals(BrokerEnvelope? other) => ReferenceEquals(this, other);
}
