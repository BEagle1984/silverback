// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Declares the internal methods of the <see cref="KafkaInboundEnvelope{TMessage,TKey}" /> not meant for public use.
/// </summary>
internal interface IInternalKafkaInboundEnvelope : IKafkaInboundEnvelope
{
    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IInternalKafkaInboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IInternalKafkaInboundEnvelope SetKey(object? key);

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="rawKey">
    ///     The serialized message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IInternalKafkaInboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IInternalKafkaInboundEnvelope SetRawKey(byte[]? rawKey);

    /// <summary>
    ///     Sets the message timestamp.
    /// </summary>
    /// <param name="timestamp">
    ///     The message timestamp.
    /// </param>
    /// <returns>
    ///     The <see cref="IInternalKafkaInboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IInternalKafkaInboundEnvelope SetTimestamp(DateTime timestamp);
}
