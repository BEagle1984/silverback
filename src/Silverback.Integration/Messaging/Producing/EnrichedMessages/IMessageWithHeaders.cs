﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.EnrichedMessages;

/// <summary>
///     Represents a message enriched with a collection of headers.
/// </summary>
public interface IMessageWithHeaders
{
    /// <summary>
    ///     Gets the message.
    /// </summary>
    object? Message { get; }

    /// <summary>
    ///     Gets the headers.
    /// </summary>
    public MessageHeaderCollection Headers { get; }
}