// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics;

/// <summary>
///     An <see cref="ISilverbackLogger{TCategoryName}" /> with some specific methods to log outbound messages
///     related events.
/// </summary>
/// <typeparam name="TCategoryName">
///     The type whose name is used for the logger category name.
/// </typeparam>
public interface IProducerLogger<out TCategoryName> : ISilverbackLogger<TCategoryName>
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
    /// <param name="endpointConfiguration">
    ///     The destination endpoint configuration.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The <see cref="IBrokerMessageIdentifier" />.
    /// </param>
    void LogProduced(
        ProducerEndpointConfiguration endpointConfiguration,
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
    /// <param name="endpointConfiguration">
    ///     The destination endpoint configuration.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="exception">
    ///     The <see cref="Exception" />.
    /// </param>
    void LogProduceError(
        ProducerEndpointConfiguration endpointConfiguration,
        IReadOnlyCollection<MessageHeader>? headers,
        Exception exception);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.OutboundMessageFiltered" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    void LogFiltered(IOutboundEnvelope envelope);

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.StoringIntoOutbox" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    void LogStoringIntoOutbox(IOutboundEnvelope envelope);

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

    /// <summary>
    ///     Logs the <see cref="IntegrationLogEvents.InvalidMessageProduced" /> event.
    /// </summary>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" />.
    /// </param>
    /// <param name="validationErrors">
    ///     The errors returned by the validation.
    /// </param>
    void LogInvalidMessage(IOutboundEnvelope envelope, string validationErrors);
}
