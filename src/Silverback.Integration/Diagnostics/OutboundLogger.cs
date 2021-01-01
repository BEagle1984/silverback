// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    internal class OutboundLogger
    {
        private readonly LogAdditionalArguments<IOutboundEnvelope> _additionalArguments;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _messageProduced;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _messageWrittenToOutbox;

        private readonly Action<ILogger, string, string?, string?, string?, string?, Exception?>
            _errorProducingOutboxStoredMessage;

        public OutboundLogger(LogAdditionalArguments<IOutboundEnvelope> additionalArguments)
        {
            _additionalArguments = Check.NotNull(additionalArguments, nameof(additionalArguments));

            _messageProduced =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.MessageProduced));

            _messageWrittenToOutbox =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.MessageWrittenToOutbox));

            _errorProducingOutboxStoredMessage =
                SilverbackLoggerMessage.Define<string, string?, string?, string?, string?>(
                    EnrichLogEvent(IntegrationLogEvents.ErrorProducingOutboxStoredMessage));
        }

        public void LogProduced(ISilverbackLogger logger, IOutboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageProduced))
                return;

            _messageProduced.Invoke(
                logger.InnerLogger,
                envelope.Endpoint.Name,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                _additionalArguments.Argument1.ValueProvider.Invoke(envelope),
                _additionalArguments.Argument2.ValueProvider.Invoke(envelope),
                null);
        }

        public void LogWrittenToOutbox(ISilverbackLogger logger, IOutboundEnvelope envelope)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.MessageWrittenToOutbox))
                return;

            _messageWrittenToOutbox.Invoke(
                logger.InnerLogger,
                envelope.Endpoint.Name,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                _additionalArguments.Argument1.ValueProvider.Invoke(envelope),
                _additionalArguments.Argument2.ValueProvider.Invoke(envelope),
                null);
        }

        public void LogErrorProducingOutboxStoredMessage(
            ISilverbackLogger logger,
            IOutboundEnvelope envelope,
            Exception exception)
        {
            if (!logger.IsEnabled(IntegrationLogEvents.ErrorProducingOutboxStoredMessage))
                return;

            _errorProducingOutboxStoredMessage.Invoke(
                logger.InnerLogger,
                envelope.Endpoint.Name,
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType),
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageId),
                _additionalArguments.Argument1.ValueProvider.Invoke(envelope),
                _additionalArguments.Argument2.ValueProvider.Invoke(envelope),
                exception);
        }

        private LogEvent EnrichLogEvent(LogEvent logEvent)
        {
            var message = _additionalArguments.EnrichMessage(
                logEvent.Message +
                " | " +
                "endpointName: {endpointName}, " +
                "messageType: {messageType}, " +
                "messageId: {messageId}");

            return new LogEvent(
                logEvent.Level,
                logEvent.EventId,
                message);
        }
    }
}
