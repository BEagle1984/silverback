// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes;

public class OutboundLoggerSubstitute<TCategory> : IOutboundLogger<TCategory>
{
    public ILogger InnerLogger { get; } = Substitute.For<ILogger>();

    public bool IsEnabled(LogEvent logEvent) => true;

    public void LogProduced(IOutboundEnvelope envelope)
    {
    }

    public void LogProduced(
        ProducerEndpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
    }

    public void LogProduceError(IOutboundEnvelope envelope, Exception exception)
    {
    }

    public void LogProduceError(
        ProducerEndpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        Exception exception)
    {
    }

    public void LogWrittenToOutbox(IOutboundEnvelope envelope)
    {
    }

    public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception)
    {
    }
}
