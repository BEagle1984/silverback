// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
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
        _loggerFactory.GetProducerLogger(envelope.Endpoint.Configuration)
            .LogProduced(this, envelope);

    public void LogProduced(
        ProducerEndpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier) =>
        _loggerFactory.GetProducerLogger(endpoint.Configuration)
            .LogProduced(this, endpoint, headers, brokerMessageIdentifier);

    public void LogProduceError(IOutboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetProducerLogger(envelope.Endpoint.Configuration)
            .LogProduceError(this, envelope, exception);

    public void LogProduceError(
        ProducerEndpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        Exception exception) =>
        _loggerFactory.GetProducerLogger(endpoint.Configuration)
            .LogProduceError(
                this,
                endpoint,
                headers,
                exception);

    public void LogFiltered(IOutboundEnvelope envelope) =>
        _loggerFactory.GetProducerLogger(envelope.Endpoint.Configuration)
            .LogFiltered(this, envelope);

    public void LogStoringIntoOutbox(IOutboundEnvelope envelope) =>
        _loggerFactory.GetProducerLogger(envelope.Endpoint.Configuration)
            .LogStoringIntoOutbox(this, envelope);

    public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetProducerLogger(envelope.Endpoint.Configuration)
            .LogErrorProducingOutboxStoredMessage(this, envelope, exception);

    public void LogInvalidMessage(IOutboundEnvelope envelope, string validationErrors) =>
        _loggerFactory.GetProducerLogger(envelope.Endpoint.Configuration).LogInvalidMessage(this, envelope, validationErrors);
}
