// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The null message handling strategies.
/// </summary>
public enum NullMessageHandlingStrategy
{
    /// <summary>
    ///     The legacy behavior prior to Silverback 3. The message is forwarded as <c>null</c> and can be subscribed
    ///     as <see cref="IInboundEnvelope{TMessage}" /> only.
    /// </summary>
    Legacy = -1,

    /// <summary>
    ///     Map the null messages to a <see cref="Tombstone{TMessage}" />.
    /// </summary>
    Tombstone = 0,

    /// <summary>
    ///     Silently skip the null message.
    /// </summary>
    Skip = 1
}
