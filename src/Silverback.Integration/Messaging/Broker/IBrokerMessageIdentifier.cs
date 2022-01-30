// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker;

/// <summary>
///     <para>
///         The primary identifier used by the message broker to recognize the exact message.
///     </para>
///     <para>
///         It can represent a Kafka offset, RabbitMQ delivery tag or other similar constructs.
///     </para>
///     <para>
///         If the message broker doesn't provide any message identifier, a local one can be created (e.g.
///         <c>Guid.NewGuid()</c>) and used later on for example to match the message to be committed.
///     </para>
/// </summary>
public interface IBrokerMessageIdentifier : IEquatable<IBrokerMessageIdentifier>
{
    /// <summary>
    ///     Gets a string that can be used to log the identifier/offset value.
    /// </summary>
    /// <remarks>
    ///     This string should contain all identifiers except the endpoint name.
    /// </remarks>
    /// <returns>
    ///     A <see cref="string" /> representing the identifier/offset value.
    /// </returns>
    string ToLogString();

    /// <summary>
    ///     Gets a string that can be used to log the identifier/offset value.
    /// </summary>
    /// <remarks>
    ///     This string must include the endpoint name, if the identifier value isn't unique across different endpoints.
    /// </remarks>
    /// <returns>
    ///     A <see cref="string" /> representing the identifier/offset value.
    /// </returns>
    string ToVerboseLogString();
}
