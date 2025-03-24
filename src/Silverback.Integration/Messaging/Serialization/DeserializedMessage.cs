// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The result of <see cref="IMessageDeserializer.DeserializeAsync" />.
/// </summary>
/// <param name="Message">
///     The deserialized message.
/// </param>
/// <param name="MessageType">
///     The message type, which should be filled with the expected type even if the message body is <c>null</c>.
/// </param>
public record struct DeserializedMessage(object? Message, Type MessageType);
