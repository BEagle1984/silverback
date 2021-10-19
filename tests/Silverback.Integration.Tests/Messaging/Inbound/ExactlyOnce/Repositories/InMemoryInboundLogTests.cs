// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Database.Model;
using Silverback.Messaging;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Inbound.ExactlyOnce.Repositories;

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
    public async Task Exists_LoggedEnvelope_TrueReturned()
    {
        Guid messageId = Guid.NewGuid();
        await _log.AddAsync(GetEnvelope());
        await _log.AddAsync(GetEnvelope(messageId));
        await _log.AddAsync(GetEnvelope());
        await _log.CommitAsync();

        bool result = await _log.ExistsAsync(GetEnvelope(messageId));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Exists_SameIdInDifferentTopic_FalseReturned()
    {
        Guid messageId = Guid.NewGuid();
        TestConsumerConfiguration configuration1 = new("topic1")
        {
            GroupId = "same"
        };
        TestConsumerConfiguration configuration2 = new("topic2")
        {
            GroupId = "same"
        };
        IRawInboundEnvelope envelope1 = GetEnvelope(messageId, configuration1.GetDefaultEndpoint());
        IRawInboundEnvelope envelope2 = GetEnvelope(messageId, configuration2.GetDefaultEndpoint());

        await _log.AddAsync(envelope1);
        await _log.CommitAsync();

        bool result = await _log.ExistsAsync(envelope2);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Exists_SameIdForDifferentConsumerGroup_FalseReturned()
    {
        Guid messageId = Guid.NewGuid();
        TestConsumerConfiguration configuration1 = new("same")
        {
            GroupId = "group1"
        };
        TestConsumerConfiguration configuration2 = new("same")
        {
            GroupId = "group2"
        };
        IRawInboundEnvelope envelope1 = GetEnvelope(messageId, configuration1.GetDefaultEndpoint());
        IRawInboundEnvelope envelope2 = GetEnvelope(messageId, configuration2.GetDefaultEndpoint());

        await _log.AddAsync(envelope1);
        await _log.CommitAsync();

        bool result = await _log.ExistsAsync(envelope2);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Exists_NotLoggedEnvelope_FalseReturned()
    {
        await _log.AddAsync(GetEnvelope());
        await _log.AddAsync(GetEnvelope());
        await _log.AddAsync(GetEnvelope());

        bool result = await _log.ExistsAsync(GetEnvelope());

        result.Should().BeFalse();
    }

    private static IRawInboundEnvelope GetEnvelope(Guid? messageId = null, ConsumerEndpoint? endpoint = null)
    {
        endpoint ??= TestConsumerEndpoint.GetDefault();

        MessageHeader[] headers = { new("x-message-id", messageId?.ToString() ?? Guid.NewGuid().ToString()) };

        return new RawInboundEnvelope(Array.Empty<byte>(), headers, endpoint, new TestOffset());
    }
}
