// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Database.Model;
using Silverback.Messaging;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLogTests
    {
        private readonly InMemoryInboundLog _log;

        public InMemoryInboundLogTests()
        {
            _log = new InMemoryInboundLog(new TransactionalListSharedItems<InboundLogEntry>());
        }

        [Fact]
        public async Task Add_SomeEnvelopesNoCommit_LogStillEmpty()
        {
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());

            (await _log.GetLengthAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Add_SomeEnvelopesAndCommit_Logged()
        {
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());

            await _log.CommitAsync();

            (await _log.GetLengthAsync()).Should().Be(3);
        }

        [Fact]
        public async Task Add_SomeEnvelopesAndRollback_LogStillEmpty()
        {
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());

            await _log.RollbackAsync();

            (await _log.GetLengthAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Exists_LoggedEnvelope_TrueIsReturned()
        {
            var messageId = Guid.NewGuid();
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope(messageId));
            await _log.AddAsync(GetEnvelope());
            await _log.CommitAsync();

            var result = await _log.ExistsAsync(GetEnvelope(messageId));

            result.Should().BeTrue();
        }

        [Fact]
        public async Task Exists_SameIdInDifferentTopic_FalseIsReturned()
        {
            var messageId = Guid.NewGuid();
            var endpoint1 = new TestConsumerEndpoint("topic1")
            {
                GroupId = "same"
            };
            var endpoint2 = new TestConsumerEndpoint("topic2")
            {
                GroupId = "same"
            };
            var envelope1 = GetEnvelope(messageId, endpoint1);
            var envelope2 = GetEnvelope(messageId, endpoint2);

            await _log.AddAsync(envelope1);
            await _log.CommitAsync();

            var result = await _log.ExistsAsync(envelope2);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task Exists_SameIdForDifferentConsumerGroup_FalseIsReturned()
        {
            var messageId = Guid.NewGuid();
            var endpoint1 = new TestConsumerEndpoint("same")
            {
                GroupId = "group1"
            };
            var endpoint2 = new TestConsumerEndpoint("same")
            {
                GroupId = "group2"
            };
            var envelope1 = GetEnvelope(messageId, endpoint1);
            var envelope2 = GetEnvelope(messageId, endpoint2);

            await _log.AddAsync(envelope1);
            await _log.CommitAsync();

            var result = await _log.ExistsAsync(envelope2);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task Exists_NotLoggedEnvelope_FalseIsReturned()
        {
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());
            await _log.AddAsync(GetEnvelope());

            var result = await _log.ExistsAsync(GetEnvelope());

            result.Should().BeFalse();
        }

        private static IRawInboundEnvelope GetEnvelope(Guid? messageId = null, IConsumerEndpoint? endpoint = null)
        {
            endpoint ??= TestConsumerEndpoint.GetDefault();

            var headers = new[]
            {
                new MessageHeader("x-message-id", messageId ?? Guid.NewGuid()),
            };

            return new RawInboundEnvelope(
                Array.Empty<byte>(),
                headers,
                endpoint,
                endpoint.Name,
                new TestOffset());
        }
    }
}
