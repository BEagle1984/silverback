// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

public class IntegrationPublisherExtensionsFixture
{
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly ProducerCollection _producers = [];

    public IntegrationPublisherExtensionsFixture()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IProducerCollection)).Returns(_producers);
        _publisher.Context.Returns(new SilverbackContext(serviceProvider));
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceEnvelopes()
    {
        TestEventOne message1 = new();
        TestEventTwo message2 = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventTwo>("two");

        await _publisher.WrapAndPublishAsync(message1);
        await _publisher.WrapAndPublishAsync(message2);

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
    public async Task WrapAndPublishAsync_ShouldProduceConfiguredEnvelopes()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        await _publisher.WrapAndPublishAsync(
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
    public async Task WrapAndPublishAsync_ShouldProduceConfiguredEnvelopes_WhenPassingArgument()
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy1) = AddProducer<TestEventOne>("one");
        (IProducer _, IProduceStrategyImplementation strategy2) = AddProducer<TestEventOne>("two");

        await _publisher.WrapAndPublishAsync(
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
    public async Task WrapAndPublishAsync_ShouldThrowOrIgnore_WhenNoMatchingProducers(bool throwIfUnhandled)
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishAsync(message, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message = new();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishAsync(
            message,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);
        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else

            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnvelopesForCollection()
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

        await _publisher.WrapAndPublishBatchAsync(messages);

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
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForCollection()
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

        await _publisher.WrapAndPublishBatchAsync(
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
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForCollection_WhenPassingArgument()
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

        await _publisher.WrapAndPublishBatchAsync(
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
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollection(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForCollectionAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        List<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnvelopesForEnumerable()
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

        await _publisher.WrapAndPublishBatchAsync(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable()
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

        await _publisher.WrapAndPublishBatchAsync(
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
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForEnumerable_WhenPassingArgument()
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

        await _publisher.WrapAndPublishBatchAsync(
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
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerable(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForEnumerableAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IEnumerable<TestEventOne> messages = [message1, message2];
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnvelopesForAsyncEnumerable()
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

        await _publisher.WrapAndPublishBatchAsync(messages);

        await strategy.Received(1).ProduceAsync(Arg.Any<IAsyncEnumerable<IOutboundEnvelope<TestEventOne>>>());
        capturedEnvelopes.ShouldNotBeNull();
        capturedEnvelopes.Should().HaveCount(2);
        capturedEnvelopes[0].Message.Should().Be(message1);
        capturedEnvelopes[0].Endpoint.RawName.Should().Be("one");
        capturedEnvelopes[1].Message.Should().Be(message2);
        capturedEnvelopes[1].Endpoint.RawName.Should().Be("one");
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable()
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

        await _publisher.WrapAndPublishBatchAsync(
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
    public async Task WrapAndPublishBatchAsync_ShouldProduceConfiguredEnvelopesForAsyncEnumerable_WhenPassingArgument()
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

        await _publisher.WrapAndPublishBatchAsync(
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
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForAsyncEnumerable(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(messages, throwIfUnhandled: throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapAndPublishBatchAsync_ShouldThrowOrIgnore_WhenNoMatchingProducersForAsyncEnumerableAndPassingArgument(bool throwIfUnhandled)
    {
        TestEventOne message1 = new();
        TestEventOne message2 = new();
        IAsyncEnumerable<TestEventOne> messages = new[] { message1, message2 }.ToAsyncEnumerable();
        (IProducer _, IProduceStrategyImplementation strategy) = AddProducer<TestEventTwo>("two");

        Func<Task> act = () => _publisher.WrapAndPublishBatchAsync(
            messages,
            (_, _) =>
            {
            },
            1,
            throwIfUnhandled);

        if (throwIfUnhandled)
            await act.Should().ThrowAsync<RoutingException>().WithMessage("No producer found for message of type 'TestEventOne'.");
        else
            await act.Should().NotThrowAsync();

        strategy.ReceivedCalls().Should().BeEmpty();
    }

    private (IProducer Producer, IProduceStrategyImplementation Strategy) AddProducer<TMessage>(string topic, bool enableSubscribing = false)
    {
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration(topic, typeof(TMessage))
            {
                Strategy = Substitute.For<IProduceStrategy>(),
                EnableSubscribing = enableSubscribing
            });
        IProduceStrategyImplementation produceStrategyImplementation = Substitute.For<IProduceStrategyImplementation>();
        producer.EndpointConfiguration.Strategy.Build(
            Arg.Any<IServiceProvider>(),
            Arg.Any<ProducerEndpointConfiguration>()).Returns(produceStrategyImplementation);
        _producers.Add(producer);
        return (producer, produceStrategyImplementation);
    }
}
