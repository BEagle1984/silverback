// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class OutboundLogger<TCategoryName> : SilverbackLogger<TCategoryName>, IOutboundLogger<TCategoryName>
{
    private readonly OutboundLoggerFactory _loggerFactory;

    public OutboundLogger(
        IMappedLevelsLogger<TCategoryName> mappedLevelsLogger,
        OutboundLoggerFactory loggerFactory)
        : base(mappedLevelsLogger)
    {
        _loggerFactory = Check.NotNull(loggerFactory, nameof(loggerFactory));
    }

    public void LogProduced(IOutboundEnvelope envelope) =>
        _loggerFactory.GetOutboundLogger(envelope.Endpoint.Configuration)
            .LogProduced(this, envelope);

    public void LogProduced(
        ProducerEndpoint actualEndpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier) =>
        _loggerFactory.GetOutboundLogger(actualEndpoint.Configuration)
            .LogProduced(this, actualEndpoint, headers, brokerMessageIdentifier);

    public void LogProduceError(IOutboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetOutboundLogger(envelope.Endpoint.Configuration)
            .LogProduceError(this, envelope, exception);

    public void LogProduceError(
        ProducerEndpoint actualEndpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        Exception exception) =>
        _loggerFactory.GetOutboundLogger(actualEndpoint.Configuration)
            .LogProduceError(
                this,
                actualEndpoint,
                headers,
                exception);

    public void LogWrittenToOutbox(IOutboundEnvelope envelope) =>
        _loggerFactory.GetOutboundLogger(envelope.Endpoint.Configuration)
            .LogWrittenToOutbox(this, envelope);

    public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception) =>
        _loggerFactory.GetOutboundLogger(envelope.Endpoint.Configuration)
            .LogErrorProducingOutboxStoredMessage(this, envelope, exception);
}
