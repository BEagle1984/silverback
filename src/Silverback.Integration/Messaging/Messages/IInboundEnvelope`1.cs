﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope" />
public interface IInboundEnvelope<out TMessage> : IInboundEnvelope
{
    /// <summary>
    ///     Gets the deserialized message body.
    /// </summary>
    new TMessage? Message { get; }
}
