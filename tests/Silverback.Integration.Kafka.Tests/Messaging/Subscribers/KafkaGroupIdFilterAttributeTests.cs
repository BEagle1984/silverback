// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Subscribers;

public class KafkaGroupIdFilterAttributeTests
{
    [Theory]
    [InlineData("group1", true)]
    [InlineData("group2", true)]
    [InlineData("group3", false)]
    public void MustProcess_InboundEnvelopesWithDifferentGroupId_ExpectedResultReturned(
        string envelopeGroupId,
        bool expectedResult)
    {
        InboundEnvelope inboundEnvelope = new(
            new MemoryStream(),
            new List<MessageHeader>(),
            new TestOffset(),
            new KafkaConsumerEndpoint(
                "my-topic",
                1,
                new KafkaConsumerConfiguration
                {
                    Client = new KafkaClientConsumerConfiguration
                    {
                        GroupId = envelopeGroupId
                    }
                }));

        bool result = new KafkaGroupIdFilterAttribute("group1", "group2").MustProcess(inboundEnvelope);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void MustProcess_NonInboundEnvelope_FalseReturned()
    {
        bool result = new KafkaGroupIdFilterAttribute().MustProcess(new NoKeyMembersMessage());

        result.Should().BeFalse();
    }

    [Fact]
    public void MustProcess_InboundEnvelopeWithNonKafkaEndpoint_FalseReturned()
    {
        InboundEnvelope inboundEnvelope = new(
            new MemoryStream(),
            new List<MessageHeader>(),
            new TestOffset(),
            new SomeConsumerEndpoint(new SomeConsumerConfiguration()));

        bool result = new KafkaGroupIdFilterAttribute().MustProcess(inboundEnvelope);

        result.Should().BeFalse();
    }

    private sealed record SomeConsumerConfiguration : ConsumerConfiguration
    {
        public override string GetUniqueConsumerGroupName() => throw new NotImplementedException();
    }

    private sealed record SomeConsumerEndpoint : ConsumerEndpoint<SomeConsumerConfiguration>
    {
        public SomeConsumerEndpoint(SomeConsumerConfiguration endpoint)
            : base("test", endpoint)
        {
        }
    }
}
