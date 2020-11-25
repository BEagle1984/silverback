// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Subscribers
{
    public class KafkaGroupIdFilterAttributeTests
    {
        [Theory]
        [InlineData("group1", true)]
        [InlineData("group2", true)]
        [InlineData("group3", false)]
        public void MustProcess_InboundEnvelopesWithDifferentGroupId_ExpectedResultIsReturned(
            string envelopeGroupId,
            bool expectedResult)
        {
            var inboundEnvelope = new InboundEnvelope(
                new MemoryStream(),
                new List<MessageHeader>(),
                new TestOffset(),
                new KafkaConsumerEndpoint("my-topic")
                {
                    Configuration =
                    {
                        GroupId = envelopeGroupId
                    }
                },
                "my-topic");

            var result = new KafkaGroupIdFilterAttribute("group1", "group2").MustProcess(inboundEnvelope);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void MustProcess_NonInboundEnvelope_FalseIsReturned()
        {
            var result = new KafkaGroupIdFilterAttribute().MustProcess(new NoKeyMembersMessage());

            result.Should().BeFalse();
        }

        [Fact]
        public void MustProcess_InboundEnvelopeWithNonKafkaEndpoint_FalseIsReturned()
        {
            var inboundEnvelope = new InboundEnvelope(
                new MemoryStream(),
                new List<MessageHeader>(),
                new TestOffset(),
                new SomeConsumerEndpoint("test"),
                string.Empty);

            var result = new KafkaGroupIdFilterAttribute().MustProcess(inboundEnvelope);

            result.Should().BeFalse();
        }

        private class SomeConsumerEndpoint : ConsumerEndpoint
        {
            public SomeConsumerEndpoint(string name)
                : base(name)
            {
            }

            public override string GetUniqueConsumerGroupName() => Name;
        }
    }
}
