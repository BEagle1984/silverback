// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Specifies how the <see cref="IMessageDeserializer" /> has to deserialize according to the message type header.
/// </summary>
public enum JsonMessageDeserializerTypeHeaderBehavior
{
    /// <summary>
    ///     Use the message type header if specified, otherwise use the configured message type.
    /// </summary>
    Optional,

    /// <summary>
    ///     Throw an exception if the consumed message doesn't specify the message type header.
    /// </summary>
    Mandatory,

    /// <summary>
    ///     Ignore the type header and deserialize according to the configured message type.
    /// </summary>
    Ignore
}
