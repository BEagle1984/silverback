// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IKafkaOutboundEnvelope" />
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
/// <typeparam name="TKey">
///     The type of the message key.
/// </typeparam>
public interface IKafkaOutboundEnvelope<out TMessage, TKey> : IKafkaOutboundEnvelope<TMessage>
{
    /// <summary>
    ///     Gets the message key.
    /// </summary>
    new TKey? Key { get; }

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IKafkaOutboundEnvelope{TKey}" /> so that additional calls can be chained.
    /// </returns>
    IKafkaOutboundEnvelope<TKey> SetKey(TKey? key);
}
