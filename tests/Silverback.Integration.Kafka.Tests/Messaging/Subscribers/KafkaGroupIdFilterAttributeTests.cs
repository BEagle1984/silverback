// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
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
                Array.Empty<byte>(),
                new List<MessageHeader>(),
                null,
                new KafkaConsumerEndpoint("my-topic")
                {
                    Configuration = new KafkaConsumerConfig
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
                Array.Empty<byte>(),
                new List<MessageHeader>(),
                null,
                new SomeConsumerEndpoint(),
                string.Empty);

            var result = new KafkaGroupIdFilterAttribute().MustProcess(inboundEnvelope);

            result.Should().BeFalse();
        }

        private class SomeConsumerEndpoint : IConsumerEndpoint
        {
            public IMessageSerializer Serializer { get; } = new JsonMessageSerializer();

            [SuppressMessage(
                "ReSharper",
                "UnassignedGetOnlyAutoProperty",
                Justification = "Unused in this implementation, but declared in interface.")]
            public EncryptionSettings? Encryption { get; }

            public string Name { get; } = string.Empty;

            public IErrorPolicy? ErrorPolicy { get; } = null!;

            public BatchSettings Batch { get; } = new BatchSettings();

            public bool ThrowIfUnhandled { get; set; }

            public void Validate()
            {
            }

            public string GetUniqueConsumerGroupName() => Name;
        }
    }
}
