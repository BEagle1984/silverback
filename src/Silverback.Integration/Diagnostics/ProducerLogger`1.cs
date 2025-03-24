// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class ProducerLogger<TCategoryName> : SilverbackLogger<TCategoryName>, IProducerLogger<TCategoryName>
{
    private readonly InternalProducerLoggerFactory _loggerFactory;

    public ProducerLogger(
        IMappedLevelsLogger<TCategoryName> mappedLevelsLogger,
        InternalProducerLoggerFactory loggerFactory)
        : base(mappedLevelsLogger)
    {
        _loggerFactory = Check.NotNull(loggerFactory, nameof(loggerFactory));
    }

    public void LogProduced(IOutboundEnvelope envelope) =>
        _loggerFactory.GetProducerLogger(envelope.EndpointConfiguration).LogProduced(this, envelope);

    public void LogProduced(
        ProducerEndpointConfiguration endpointConfiguration,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier) =>
        _loggerFactory.GetProducerLogger(endpointConfiguration)
            .LogProduced(this, endpointConfiguration, headers, brokerMessageIdentifier);

    public void LogProduceError(IOutboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetProducerLogger(envelope.EndpointConfiguration)
            .LogProduceError(this, envelope, exception);

    public void LogProduceError(
        ProducerEndpointConfiguration endpointConfiguration,
        IReadOnlyCollection<MessageHeader>? headers,
        Exception exception) =>
        _loggerFactory.GetProducerLogger(endpointConfiguration)
            .LogProduceError(this, endpointConfiguration, headers, exception);

    public void LogFiltered(IOutboundEnvelope envelope) =>
        _loggerFactory.GetProducerLogger(envelope.EndpointConfiguration)
            .LogFiltered(this, envelope);

    public void LogStoringIntoOutbox(IOutboundEnvelope envelope) =>
        _loggerFactory.GetProducerLogger(envelope.EndpointConfiguration)
            .LogStoringIntoOutbox(this, envelope);

    public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetProducerLogger(envelope.EndpointConfiguration)
            .LogErrorProducingOutboxStoredMessage(this, envelope, exception);

    public void LogInvalidMessage(IOutboundEnvelope envelope, string validationErrors) =>
        _loggerFactory.GetProducerLogger(envelope.EndpointConfiguration).LogInvalidMessage(this, envelope, validationErrors);
}
