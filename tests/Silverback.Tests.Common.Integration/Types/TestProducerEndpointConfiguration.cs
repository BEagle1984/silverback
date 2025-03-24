// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;

namespace Silverback.Tests.Types;

public sealed record TestProducerEndpointConfiguration : ProducerEndpointConfiguration<TestProducerEndpoint>
{
    public TestProducerEndpointConfiguration()
    {
    }

    public TestProducerEndpointConfiguration(string topic, Type? messageType = null)
    {
        EndpointResolver = new TestStaticProducerEndpointResolver(topic);

        if (messageType != null)
            MessageType = messageType;
    }

    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A new instance is desired")]
    public static TestProducerEndpointConfiguration GetDefault() => new()
    {
        EndpointResolver = new TestStaticProducerEndpointResolver("test")
    };

    public TestProducerEndpoint GetDefaultEndpoint() =>
        (TestProducerEndpoint)EndpointResolver.GetEndpoint(
            new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                null,
                this,
                Substitute.For<IProducer>()));
}
