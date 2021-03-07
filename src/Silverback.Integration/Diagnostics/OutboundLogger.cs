// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    internal class OutboundLogger
    {
        private readonly IBrokerLogEnricher _logEnricher;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _messageProduced;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _errorProducingMessage;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _messageWrittenToOutbox;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _errorProducingOutboxStoredMessage;

        public OutboundLogger(IBrokerLogEnricher logEnricher)
        {
            _logEnricher = Check.NotNull(logEnricher, nameof(logEnricher));

            _messageProduced =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    IntegrationLogEvents.MessageProduced.Enrich(_logEnricher));

            _errorProducingMessage =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    IntegrationLogEvents.ErrorProducingMessage.Enrich(_logEnricher));

            _messageWrittenToOutbox =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    IntegrationLogEvents.MessageWrittenToOutbox.Enrich(_logEnricher));

            _errorProducingOutboxStoredMessage =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    IntegrationLogEvents.ErrorProducingOutboxStoredMessage.Enrich(_logEnricher));
        }

        public void LogProduced(ISilverbackLogger logger, IOutboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _messageProduced.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogProduced(
            ISilverbackLogger logger,
            IProducerEndpoint endpoint,
            string actualEndpointName,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                endpoint,
                headers,
                brokerMessageIdentifier);

            _messageProduced.Invoke(
                logger.InnerLogger,
                actualEndpointName,
                headers?.GetValue(DefaultMessageHeaders.MessageType),
                headers?.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                null);
        }

        public void LogProduceError(ISilverbackLogger logger, IOutboundEnvelope envelope, Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                null);

            _errorProducingMessage.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                exception);
        }

        public void LogProduceError(
            ISilverbackLogger logger,
            IProducerEndpoint endpoint,
            string actualEndpointName,
            IReadOnlyCollection<MessageHeader>? headers,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                endpoint,
                headers,
                null);

            _errorProducingMessage.Invoke(
                logger.InnerLogger,
                actualEndpointName,
                headers?.GetValue(DefaultMessageHeaders.MessageType),
                headers?.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                exception);
        }

        public void LogWrittenToOutbox(ISilverbackLogger logger, IOutboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageWrittenToOutbox))
                return;

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _messageWrittenToOutbox.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
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

            (string? value1, string? value2) = _logEnricher.GetAdditionalValues(
                envelope.Endpoint,
                envelope.Headers,
                envelope.BrokerMessageIdentifier);

            _errorProducingOutboxStoredMessage.Invoke(
                logger.InnerLogger,
                envelope.ActualEndpointName,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                value1,
                value2,
                exception);
        }
    }
}
