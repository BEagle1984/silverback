// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Filter;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Filter;

public class GenericInboundMessageFilterTests
{
    [Fact]
    public async Task ShouldProcessAsync_ShouldEvaluateMessageFilter()
    {
        InboundEnvelope<TestEventOne> envelope1 = CreateEnvelope(new TestEventOne { Content = "yes" });
        InboundEnvelope<TestEventOne> envelope2 = CreateEnvelope(new TestEventOne { Content = "no" });

        GenericInboundMessageFilter<TestEventOne> filter = new(testEventOne => testEventOne?.Content == "yes");

        (await filter.ShouldProcessAsync(envelope1, CancellationToken.None)).ShouldBeTrue();
        (await filter.ShouldProcessAsync(envelope2, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldProcessAsync_ShouldEvaluateEnvelopeFilter()
    {
        InboundEnvelope<TestEventOne> envelope1 = CreateEnvelope(new TestEventOne { Content = "yes" });
        InboundEnvelope<TestEventOne> envelope2 = CreateEnvelope(new TestEventOne { Content = "no" });

        GenericInboundMessageFilter<TestEventOne> filter = new(envelope => envelope.Message?.Content == "yes");

        (await filter.ShouldProcessAsync(envelope1, CancellationToken.None)).ShouldBeTrue();
        (await filter.ShouldProcessAsync(envelope2, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldProcessAsync_ShouldEvaluateFilter_WhenMessageIsOfDerivedType()
    {
        InboundEnvelope<TestEventOne> envelope1 = CreateEnvelope(new TestEventOne { Content = "yes" });
        InboundEnvelope<TestEventOne> envelope2 = CreateEnvelope(new TestEventOne { Content = "no" });

        GenericInboundMessageFilter<IIntegrationEvent> filter = new(
            (IIntegrationEvent? message) => message is TestEventOne { Content: "yes" });

        (await filter.ShouldProcessAsync(envelope1, CancellationToken.None)).ShouldBeTrue();
        (await filter.ShouldProcessAsync(envelope2, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldProcessAsync_ShouldReturnTrue_WhenMessageTypeMismatch()
    {
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope(new TestEventOne { Content = "no" });
        bool isFilterInvoked = false;

        GenericInboundMessageFilter<TestEventTwo> filter = new(
            (TestEventTwo? _) =>
            {
                isFilterInvoked = true;
                return false;
            });

        (await filter.ShouldProcessAsync(envelope, CancellationToken.None)).ShouldBeTrue();
        isFilterInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldProcessAsync_ShouldPassNullToMessageFilter_WhenMessageIsTombstone()
    {
        IInboundEnvelope envelope = await DeserializeTombstoneAsync();
        List<TestEventOne?> filteredMessages = [];

        GenericInboundMessageFilter<TestEventOne> filter = new(
            message =>
            {
                filteredMessages.Add(message);
                return message != null;
            });

        (await filter.ShouldProcessAsync(envelope, CancellationToken.None)).ShouldBeFalse();
        filteredMessages.ShouldBe([null]);
    }

    [Fact]
    public async Task ShouldProcessAsync_ShouldPassTombstoneEnvelopeToEnvelopeFilter()
    {
        IInboundEnvelope envelope = await DeserializeTombstoneAsync();

        GenericInboundMessageFilter<TestEventOne> filter = new(inboundEnvelope => !inboundEnvelope.IsTombstone);

        (await filter.ShouldProcessAsync(envelope, CancellationToken.None)).ShouldBeFalse();
    }

    private static InboundEnvelope<TestEventOne> CreateEnvelope(TestEventOne message) =>
        new(message, null, null, TestConsumerEndpoint.GetDefault(), Substitute.For<IConsumer>(), new TestOffset());

    // Deserializes an empty message as the consumer pipeline does for an endpoint consuming TestEventOne
    private static async Task<IInboundEnvelope> DeserializeTombstoneAsync()
    {
        TestConsumerEndpoint endpoint = new TestConsumerEndpointConfiguration("test")
        {
            Deserializer = new JsonMessageDeserializer<TestEventOne>()
        }.GetDefaultEndpoint();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            new RawInboundEnvelope(Stream.Null, [], endpoint, Substitute.For<IConsumer>(), new TestOffset()));

        await new DeserializerConsumerBehavior().HandleAsync(context, (_, _) => default, CancellationToken.None);

        return context.Envelope.ShouldBeAssignableTo<IInboundEnvelope>().ShouldNotBeNull();
    }
}
