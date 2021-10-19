// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics;

/// <summary>
///     An <see cref="ISilverbackLogger{TCategoryName}" /> with some specific methods to log outbound messages
///     related events.
/// </summary>
/// <typeparam name="TCategoryName">
///     The type who's name is used for the logger category name.
/// </typeparam>
public interface IOutboundLogger<out TCategoryName> : ISilverbackLogger<TCategoryName>
{
    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.MessageProduced" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    void LogProduced(IOutboundEnvelope envelope);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.MessageProduced" /> event.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The <see cref="IBrokerMessageIdentifier" />.
    /// </param>
    void LogProduced(
        ProducerEndpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.ErrorProducingMessage" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    /// <param name="exception">
    ///     The <see cref="Exception" />.
    /// </param>
    void LogProduceError(IOutboundEnvelope envelope, Exception exception);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.ErrorProducingMessage" /> event.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="exception">
    ///     The <see cref="Exception" />.
    /// </param>
    void LogProduceError(ProducerEndpoint endpoint, IReadOnlyCollection<MessageHeader>? headers, Exception exception);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.MessageWrittenToOutbox" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    void LogWrittenToOutbox(IOutboundEnvelope envelope);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.ErrorProducingOutboxStoredMessage" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    /// <param name="exception">
    ///     The <see cref="Exception" />.
    /// </param>
    void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception);
}
