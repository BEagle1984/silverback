// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class RawInboundEnvelopeTests
{
    [Fact]
    public void Constructor_NullRawMessage_NoExceptionIsThrown()
    {
        RawInboundEnvelope envelope = new(
            (Stream?)null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.Should().NotBeNull();
    }
}
