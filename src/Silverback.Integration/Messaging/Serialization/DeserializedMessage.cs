// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The result of <see cref="IMessageSerializer.DeserializeAsync" />.
/// </summary>
/// <param name="Message">
///     The deserialized message.
/// </param>
/// <param name="MessageType">
///     The message type, which should be filled with the expected type even if the message body is <c>null</c>.
/// </param>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "False positive, remove suppression once record struct is handled properly")]
public record DeserializedMessage(object? Message, Type MessageType);
