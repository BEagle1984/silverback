// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class RawInboundEnvelopeTests
    {
        [Fact]
        public void Constructor_NullRawMessage_NoExceptionIsThrown()
        {
            var envelope = new RawInboundEnvelope(
                (Stream?)null,
                null,
                TestConsumerEndpoint.GetDefault(),
                "test",
                new TestOffset("a", "b"));

            envelope.Should().NotBeNull();
        }
    }
}
