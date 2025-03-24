// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Configuration;

/// <content>
///     Implements the <c>HandleMessagesOfType</c> methods.
/// </content>
public partial class SilverbackBuilder
{
    /// <summary>
    ///     Configures the type <typeparamref name="TMessage" /> to be recognized as a message to enable features like automatic republishing.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The (base) message type.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder HandleMessagesOfType<TMessage>() =>
        HandleMessagesOfType(typeof(TMessage));

    /// <summary>
    ///     Configures the specified type to be recognized as a message to enable features like automatic republishing.
    /// </summary>
    /// <param name="messageType">
    ///     The (base) message type.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder HandleMessagesOfType(Type messageType)
    {
        if (!BusOptions.MessageTypes.Contains(messageType))
            BusOptions.MessageTypes.Add(messageType);

        return this;
    }
}
