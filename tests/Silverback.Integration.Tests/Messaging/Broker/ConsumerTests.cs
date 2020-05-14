// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class ConsumerTests
    {
        private readonly TestBroker _broker;

        private readonly SilverbackEventsSubscriber _silverbackEventsSubscriber;

        public ConsumerTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddInMemoryChunkStore())
                .AddSingletonSubscriber<SilverbackEventsSubscriber>();

            var serviceProvider = services.BuildServiceProvider();

            _broker = serviceProvider.GetRequiredService<TestBroker>();
            _silverbackEventsSubscriber = serviceProvider.GetRequiredService<SilverbackEventsSubscriber>();
        }

        [Fact]
        public async Task HandleMessage_SomeMessages_MessagesReceived()
        {
            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer)_broker.GetConsumer(
                TestConsumerEndpoint.GetDefault(),
                args => envelopes.AddRange(args.Envelopes));
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne());
            await consumer.TestHandleMessage(new TestEventTwo());
            await consumer.TestHandleMessage(new TestEventOne());
            await consumer.TestHandleMessage(new TestEventTwo());
            await consumer.TestHandleMessage(new TestEventTwo());

            envelopes.Count.Should().Be(5);
        }

        [Fact]
        public async Task HandleMessage_MessageCorrectlyProcessed_ConsumingCompletedEventPublished()
        {
            var consumer = (TestConsumer)_broker.GetConsumer(
                TestConsumerEndpoint.GetDefault(),
                args =>
                {
                    /* processing */
                });
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne());
            await consumer.TestHandleMessage(new TestEventTwo());
            await consumer.TestHandleMessage(new TestEventOne());
            await consumer.TestHandleMessage(new TestEventTwo());
            await consumer.TestHandleMessage(new TestEventTwo());

            _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingCompletedEvent>()
                .Count().Should().Be(5);
        }

        [Fact]
        public async Task HandleMessage_SomeMessagesFailToBeProcessed_ConsumingCompletedAndAbortedEventsPublished()
        {
            var consumer = (TestConsumer)_broker.GetConsumer(
                TestConsumerEndpoint.GetDefault(),
                args =>
                {
                    if (args.Envelopes.First() is IInboundEnvelope<TestEventOne>)
                        throw new InvalidOperationException();
                });
            _broker.Connect();

            try
            {
                await consumer.TestHandleMessage(new TestEventOne());
            }
            catch (Exception)
            {
                // ignored
            }

            await consumer.TestHandleMessage(new TestEventTwo());

            try
            {
                await consumer.TestHandleMessage(new TestEventOne());
            }
            catch (Exception)
            {
                // ignored
            }

            await consumer.TestHandleMessage(new TestEventTwo());
            await consumer.TestHandleMessage(new TestEventTwo());

            _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingCompletedEvent>()
                .Count().Should().Be(3);
            _silverbackEventsSubscriber.ReceivedEvents.OfType<ConsumingAbortedEvent>()
                .Count().Should().Be(2);
        }

        [Fact]
        public async Task HandleMessage_SomeMessages_HeadersReceivedWithInboundMessages()
        {
            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer)_broker.GetConsumer(
                TestConsumerEndpoint.GetDefault(),
                args => envelopes.AddRange(args.Envelopes));
            _broker.Connect();

            await consumer.TestHandleMessage(
                new TestEventOne(),
                new[] { new MessageHeader { Key = "key", Value = "value1" } });
            await consumer.TestHandleMessage(
                new TestEventOne(),
                new[] { new MessageHeader { Key = "key", Value = "value2" } });

            var firstMessage = envelopes.First();
            firstMessage.Headers.Count.Should().Be(2);
            firstMessage.Headers.Select(h => h.Key).Should().BeEquivalentTo("key", "x-message-type");
            firstMessage.Headers.GetValue("key").Should().Be("value1");
            var secondMessage = envelopes.Skip(1).First();
            secondMessage.Headers.Count.Should().Be(2);
            secondMessage.Headers.Select(h => h.Key).Should().BeEquivalentTo("key", "x-message-type");
            secondMessage.Headers.GetValue("key").Should().Be("value2");
        }

        [Fact]
        public async Task HandleMessage_SomeMessages_FailedAttemptsReceivedWithInboundMessages()
        {
            var envelopes = new List<IRawInboundEnvelope>();
            var consumer = (TestConsumer)_broker.GetConsumer(
                TestConsumerEndpoint.GetDefault(),
                args => envelopes.AddRange(args.Envelopes));
            _broker.Connect();

            await consumer.TestHandleMessage(new TestEventOne());
            await consumer.TestHandleMessage(
                new TestEventOne(),
                new[] { new MessageHeader { Key = DefaultMessageHeaders.FailedAttempts, Value = "3" } });

            envelopes.First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(null);
            envelopes.Skip(1).First().Headers.GetValue<int>(DefaultMessageHeaders.FailedAttempts).Should().Be(3);
        }
    }
}
