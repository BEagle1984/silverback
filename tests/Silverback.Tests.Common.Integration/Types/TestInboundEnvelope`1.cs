// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Tests.Types;

internal record TestInboundEnvelope<TMessage> : InboundEnvelope<TMessage>, ITestInboundEnvelope
    where TMessage : class
{
    public TestInboundEnvelope(
        TMessage? message,
        Stream? rawMessage,
        IEnumerable<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : this(message, rawMessage, endpoint, consumer, brokerMessageIdentifier)
    {
        if (headers != null)
            Headers.AddRange(headers);
    }

    public TestInboundEnvelope(
        TMessage? message,
        Stream? rawMessage,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(message, rawMessage, endpoint, consumer, brokerMessageIdentifier)
    {
    }

    public TestInboundEnvelope(TMessage? message, IInboundEnvelope clonedEnvelope)
        : base(message, clonedEnvelope)
    {
        ITestInboundEnvelope clonedTestEnvelope = Check.IsOfType<ITestInboundEnvelope>(clonedEnvelope, nameof(clonedEnvelope));

        Key = clonedTestEnvelope.Key;
    }

    public object? Key { get; init; }

    public override string? GetKey() => Key?.ToString();
}
