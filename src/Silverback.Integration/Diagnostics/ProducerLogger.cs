// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class ProducerLogger
{
    private readonly IBrokerLogEnricher _logEnricher;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _messageProduced;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _errorProducingMessage;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _messageFiltered;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _storingIntoOutbox;

    private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?> _errorProducingOutboxStoredMessage;

    private readonly Action<ILogger, string, string, string?, string?, string?, string?, Exception?> _invalidMessageProduced;

    public ProducerLogger(IBrokerLogEnricher logEnricher)
    {
        _logEnricher = Check.NotNull(logEnricher, nameof(logEnricher));

        _messageProduced = _logEnricher.Define(IntegrationLogEvents.MessageProduced);
        _errorProducingMessage = _logEnricher.Define(IntegrationLogEvents.ErrorProducingMessage);
        _messageFiltered = _logEnricher.Define(IntegrationLogEvents.OutboundMessageFiltered);
        _storingIntoOutbox = _logEnricher.Define(IntegrationLogEvents.StoringIntoOutbox);
        _errorProducingOutboxStoredMessage = _logEnricher.Define(IntegrationLogEvents.ErrorProducingOutboxStoredMessage);
        _invalidMessageProduced = _logEnricher.Define<string>(IntegrationLogEvents.InvalidMessageProduced);
    }

    public void LogProduced(ISilverbackLogger logger, IOutboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(envelope.Headers, envelope.BrokerMessageIdentifier);

        _messageProduced.Invoke(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogProduced(
        ISilverbackLogger logger,
        ProducerEndpointConfiguration endpointConfiguration,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(headers, brokerMessageIdentifier);

        _messageProduced.Invoke(
            logger.InnerLogger,
            endpointConfiguration.DisplayName,
            headers?.GetValue(DefaultMessageHeaders.MessageType),
            headers?.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogProduceError(
        ISilverbackLogger logger,
        IOutboundEnvelope envelope,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ErrorProducingMessage))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(envelope.Headers, envelope.BrokerMessageIdentifier);

        _errorProducingMessage.Invoke(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName,
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogProduceError(
        ISilverbackLogger logger,
        ProducerEndpointConfiguration endpointConfiguration,
        IReadOnlyCollection<MessageHeader>? headers,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ErrorProducingMessage))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(headers, null);

        _errorProducingMessage.Invoke(
            logger.InnerLogger,
            endpointConfiguration.DisplayName,
            headers?.GetValue(DefaultMessageHeaders.MessageType),
            headers?.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogFiltered(ISilverbackLogger logger, IOutboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.OutboundMessageFiltered))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(envelope.Headers, envelope.BrokerMessageIdentifier);

        _messageFiltered.Invoke(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName, // TODO: Does it make sense to add the display name here?
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogStoringIntoOutbox(ISilverbackLogger logger, IOutboundEnvelope envelope)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.StoringIntoOutbox))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(envelope.Headers, envelope.BrokerMessageIdentifier);

        _storingIntoOutbox.Invoke(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName, // TODO: Does it make sense to add the display name here?
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }

    public void LogErrorProducingOutboxStoredMessage(
        ISilverbackLogger logger,
        IOutboundEnvelope envelope,
        Exception exception)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.ErrorProducingOutboxStoredMessage))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(envelope.Headers, envelope.BrokerMessageIdentifier);

        _errorProducingOutboxStoredMessage.Invoke(
            logger.InnerLogger,
            envelope.EndpointConfiguration.DisplayName, // TODO: Does it make sense to add the display name here?
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            exception);
    }

    public void LogInvalidMessage(ISilverbackLogger logger, IOutboundEnvelope envelope, string validationErrors)
    {
        if (!logger.IsEnabled(IntegrationLogEvents.InvalidMessageConsumed))
            return;

        (string? value1, string? value2) = _logEnricher.GetAdditionalValues(envelope.Headers, envelope.BrokerMessageIdentifier);

        _invalidMessageProduced.Invoke(
            logger.InnerLogger,
            validationErrors,
            envelope.EndpointConfiguration.DisplayName, // TODO: Does it make sense to add the display name here?
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
            envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
            value1,
            value2,
            null);
    }
}
