// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

[SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "Testing sync methods")]
public partial class IntegrationPublisherExtensionsFixture
{
    [Fact]
    public async Task WrapAndPublish_ShouldProduceEnvelopes()
    {
        TestEventOne message1 = new();
        TestEventTwo message2 = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventTwo>("two");

        _publisher.WrapAndPublish(message1);
        _publisher.WrapAndPublish(message2);

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message1 && envelope.Endpoint.RawName == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventTwo>>(
                envelope =>
                    envelope.Message == message2 && envelope.Endpoint.RawName == "two"));
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceConfiguredEnvelopes()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish(
            message,
            envelope =>
            {
                envelope.Headers.Add("test", "value");
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Fact]
    public async Task WrapAndPublish_ShouldProduceConfiguredEnvelopes_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        _publisher.WrapAndPublish(
            message,
            static (envelope, value) =>
            {
                envelope.Headers.Add("test", value);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            "value");

        await strategy1.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "one" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "one"));
        await strategy2.Received(1).ProduceAsync(
            Arg.Is<IOutboundEnvelope<TestEventOne>>(
                envelope =>
                    envelope.Message == message && envelope.Endpoint.RawName == "two" &&
                    envelope.Headers.Contains("test") &&
                    envelope.Headers["x-topic"] == "two"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublish_ShouldThrowOrIgnore_WhenNoMatchingProducers(bool throwIfUnhandled)
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublish(message, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublish_ShouldThrowOrIgnore_WhenNoMatchingProducersAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublish(
            message,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);
        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else

            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArray()));

        _publisher.WrapAndPublishBatch(messages);

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(2);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForCollection()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArray()));
        int count = 0;

        _publisher.WrapAndPublishBatch(
            messages,
            envelope =>
            {
                envelope.Headers.Add("x-index", ++count);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(2);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].Headers["x-index"].Should().Be("3");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Headers["x-index"].Should().Be("4");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForCollection_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes1 = null;
        await strategy1.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes1 = envelopes.ToArray()));
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes2 = null;
        await strategy2.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes2 = envelopes.ToArray()));

        _publisher.WrapAndPublishBatch(
            messages,
            static (envelope, counter) =>
            {
                envelope.Headers.Add("x-index", counter.Increment());
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            new Counter());

        await strategy1.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes1.ShouldNotBeNull();
        capturedEnvelopes1.Should().HaveCount(2);
        capturedEnvelopes1[0].Message.Should().Be(message1);
        capturedEnvelopes1[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes1[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes1[1].Message.Should().Be(message2);
        capturedEnvelopes1[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes1[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes1[1].Headers["x-topic"].Should().Be("one");

        await strategy2.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes2.ShouldNotBeNull();
        capturedEnvelopes2.Should().HaveCount(2);
        capturedEnvelopes2[0].Message.Should().Be(message1);
        capturedEnvelopes2[0].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[0].Headers["x-index"].Should().Be("3");
        capturedEnvelopes2[0].Headers["x-topic"].Should().Be("two");
        capturedEnvelopes2[1].Message.Should().Be(message2);
        capturedEnvelopes2[1].Endpoint.RawName.Should().Be("two");
        capturedEnvelopes2[1].Headers["x-index"].Should().Be("4");
        capturedEnvelopes2[1].Headers["x-topic"].Should().Be("two");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollection(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollectionAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));

        _publisher.WrapAndPublishBatch(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));
        int count = 0;

        _publisher.WrapAndPublishBatch(
            messages,
            envelope =>
            {
                envelope.Headers.Add("x-index", ++count);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArray()));

        _publisher.WrapAndPublishBatch(
            messages,
            static (envelope, counter) =>
            {
                envelope.Headers.Add("x-index", counter.Increment());
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerable(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerableAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceEnvelopesForAsyncEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForAsyncEnumerable()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));
        int count = 0;

        _publisher.WrapAndPublishBatch(
            messages,
            envelope =>
            {
                envelope.Headers.Add("x-index", ++count);
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            });

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldProduceConfiguredEnvelopesForAsyncEnumerable_WhenPassingArgument()
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventOne>("one");
        IOutboundEnvelope<TestEventOne>[]? capturedEnvelopes = null;
        await strategy.ProduceAsync(
            Arg.Do<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>(
                envelopes =>
                    capturedEnvelopes = envelopes.ToArrayAsync().SafeWait()));

        _publisher.WrapAndPublishBatch(
            messages,
            static (envelope, counter) =>
            {
                envelope.Headers.Add("x-index", counter.Increment());
                envelope.Headers.Add("x-topic", envelope.Endpoint.RawName);
            },
            new Counter());

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[0].Headers["x-index"].Should().Be("1");
        capturedEnvelopes[0].Headers["x-topic"].Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Headers["x-index"].Should().Be("2");
        capturedEnvelopes[1].Headers["x-topic"].Should().Be("one");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForAsyncEnumerable(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrapAndPublishBatch_ShouldThrowOrIgnore_WhenNoMatchingProducersForAsyncEnumerableAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Action act = () => _publisher.WrapAndPublishBatch(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            act.Should().Throw<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            act.Should().NotThrow();

        strategy.ReceivedCalls().Should().BeEmpty();
    }
}
